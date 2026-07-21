using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.External;

// OANVÄND. Livsmedelsverkets v1-API svarar 200 OK men returnerar noll
// poster (totalRecords = 0). Appen använder LocalNutritionDatabase i
// stället. Klassen ligger kvar ifall API:et fylls med data - då räcker
// det att byta registrering i MauiProgram.
public class LivsmedelsverketClient : INutritionLookup
{
    // Listan är paginerad. Hela databasen är ~2 600 poster, så några
    // sidor räcker - sedan cachas allt lokalt och söks utan nätverk.
    private const int PageSize = 500;
    private const int MaxPages = 20;
    private const int CacheDays = 30;

    // 1 = svenska, 2 = engelska
    private const int Language = 1;

    private readonly HttpClient _http;
    private readonly string _cachePath;

    private List<CachedFood>? _cache;

    public LivsmedelsverketClient(HttpClient http, string cacheDirectory)
    {
        _http = http;
        _cachePath = Path.Combine(cacheDirectory, "livsmedelsverket-cache.json");
    }

    public string Attribution => "Källa: Livsmedelsverkets livsmedelsdatabas";

    // ==================================================================
    //  MAPPNING MOT API:ET
    //  Svaren är kuvert: { "_meta": {...}, "_links": [...], "livsmedel": [] }
    // ==================================================================

    private static string ListEndpoint(int offset, int limit) =>
        $"livsmedel?offset={offset}&limit={limit}&sprak={Language}";

    private static string NutrientEndpoint(string nummer) =>
        $"livsmedel/{nummer}/naringsvarden?sprak={Language}";

    private static double? Pick(IEnumerable<NutrientDto> values, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var hit = values.FirstOrDefault(v =>
                v.Namn is not null &&
                v.Namn.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));

            if (hit?.Varde is double v) return v;
        }

        return null;
    }

    private static NutritionLookupResult MapNutrients(
        string nummer, string namn, List<NutrientDto> values)
    {
        // Salt anges ibland som natrium i mg. Räkna om vid behov:
        // natrium * 2,5 = salt, och mg -> g.
        double? salt = Pick(values, "Salt");

        if (salt is null)
        {
            var sodium = values.FirstOrDefault(v =>
                v.Namn is not null &&
                v.Namn.StartsWith("Natrium", StringComparison.OrdinalIgnoreCase));

            if (sodium?.Varde is double amount)
            {
                bool inMilligrams =
                    sodium.Enhet?.Equals("mg", StringComparison.OrdinalIgnoreCase) ?? true;

                salt = (inMilligrams ? amount / 1000.0 : amount) * 2.5;
            }
        }

        return new NutritionLookupResult(
            ExternalId: nummer,
            Name: namn,
            // API:et lämnar ingen gruppering i näringsvärdessvaret,
            // så användaren får välja kategori själv
            Category: FoodCategory.Other,
            CaloriesPer100g: Pick(values, "Energi (kcal)", "Energi, kcal", "Energi"),
            ProteinPer100g: Pick(values, "Protein"),
            FatPer100g: Pick(values, "Fett, totalt", "Fett"),
            CarbsPer100g: Pick(values, "Kolhydrater, tillgängliga", "Kolhydrater"),
            FiberPer100g: Pick(values, "Fibrer", "Fiber"),
            SaltPer100g: salt);
    }

    // ==================================================================

    public async Task<IReadOnlyList<NutritionSearchHit>> SearchAsync(
        string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return Array.Empty<NutritionSearchHit>();

        var all = await EnsureCacheAsync(ct);
        var q = query.Trim();

        return all
            .Where(f => f.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(f => f.Name.Length)
            .Take(15)
            .Select(f => new NutritionSearchHit(f.Id, f.Name))
            .ToList();
    }

    public async Task<NutritionLookupResult?> GetAsync(
        string externalId, CancellationToken ct = default)
    {
        var all = await EnsureCacheAsync(ct);
        var food = all.FirstOrDefault(f => f.Id == externalId);
        if (food is null) return null;

        using var response = await _http.GetAsync(NutrientEndpoint(externalId), ct);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        var envelope = await JsonSerializer.DeserializeAsync<NutrientEnvelope>(
            stream, JsonOptions, ct);

        var values = envelope?.Naringsvarden;
        if (values is null || values.Count == 0) return null;

        return MapNutrients(externalId, food.Name, values);
    }

    // ----- Cache -----

    private async Task<List<CachedFood>> EnsureCacheAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;

        if (File.Exists(_cachePath))
        {
            var age = DateTime.Now - File.GetLastWriteTime(_cachePath);

            if (age.TotalDays < CacheDays)
            {
                try
                {
                    await using var file = File.OpenRead(_cachePath);
                    _cache = await JsonSerializer.DeserializeAsync<List<CachedFood>>(
                        file, JsonOptions, ct);

                    if (_cache is { Count: > 0 }) return _cache;
                }
                catch
                {
                    // Trasig cachefil - hämta om i stället för att krascha
                }
            }
        }

        _cache = await FetchAllPagesAsync(ct);

        // Tom lista cachas inte - då hade ett misslyckat anrop låst
        // appen i trettio dagar
        if (_cache.Count > 0)
            await SaveCacheAsync(ct);

        return _cache;
    }

    private async Task<List<CachedFood>> FetchAllPagesAsync(CancellationToken ct)
    {
        var all = new List<CachedFood>();

        for (int page = 0; page < MaxPages; page++)
        {
            int offset = page * PageSize;

            using var response = await _http.GetAsync(ListEndpoint(offset, PageSize), ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);

            var envelope = await JsonSerializer.DeserializeAsync<FoodEnvelope>(
                stream, JsonOptions, ct);

            var items = envelope?.Livsmedel ?? new List<FoodDto>();

            all.AddRange(items
                .Where(i => i.Nummer is not null && !string.IsNullOrWhiteSpace(i.Namn))
                .Select(i => new CachedFood(i.Nummer!.ToString()!, i.Namn!)));

            if (items.Count < PageSize) break;
            if (envelope?.Meta is { TotalRecords: > 0 } m && all.Count >= m.TotalRecords) break;
        }

        return all;
    }

    private async Task SaveCacheAsync(CancellationToken ct)
    {
        try
        {
            await using var file = File.Create(_cachePath);
            await JsonSerializer.SerializeAsync(file, _cache, JsonOptions, ct);
        }
        catch
        {
            // Cachen är en bekvämlighet, inte ett krav
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // ----- DTO:er som speglar API-svaret -----

    private record CachedFood(string Id, string Name);

    private class MetaDto
    {
        public int TotalRecords { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
    }

    private class FoodEnvelope
    {
        [JsonPropertyName("_meta")]
        public MetaDto? Meta { get; set; }

        public List<FoodDto>? Livsmedel { get; set; }
    }

    private class NutrientEnvelope
    {
        [JsonPropertyName("_meta")]
        public MetaDto? Meta { get; set; }

        public List<NutrientDto>? Naringsvarden { get; set; }
    }

    private class FoodDto
    {
        // object, inte string - numret kan komma som tal eller sträng
        public object? Nummer { get; set; }
        public string? Namn { get; set; }
    }

    private class NutrientDto
    {
        public string? Namn { get; set; }
        public double? Varde { get; set; }
        public string? Enhet { get; set; }
        public string? Forkortning { get; set; }
    }
}