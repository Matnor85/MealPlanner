using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.External;

// Läser näringsvärden ur en lokal fil som följer med appen.
// Livsmedelsverkets v1-API returnerar i skrivande stund noll poster,
// så data kommer i stället från deras nedladdningsbara databas.
public class LocalNutritionDatabase : INutritionLookup
{
    private const int MaxResults = 25;

    private readonly Func<Task<Stream>> _openFile;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<FoodRow>? _rows;

    public LocalNutritionDatabase(Func<Task<Stream>> openFile) => _openFile = openFile;

    public string Attribution => "Källa: Livsmedelsverkets livsmedelsdatabas, version 2026-07-01";

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var rows = await EnsureLoadedAsync(ct);
        return rows.Count;
    }

    public async Task<IReadOnlyList<NutritionSearchHit>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await EnsureLoadedAsync(ct);
        return rows.Select(r => new NutritionSearchHit(r.Id, r.N)).ToList();
    }

    public async Task<IReadOnlyList<NutritionSearchHit>> SearchAsync(
        string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return Array.Empty<NutritionSearchHit>();

        var rows = await EnsureLoadedAsync(ct);
        var q = query.Trim();

        return rows
            .Where(r => r.N.Contains(q, StringComparison.OrdinalIgnoreCase))
            // Exakt namn först, sedan de som börjar på söktexten,
            // sist de där träffen ligger inne i namnet.
            .OrderBy(r => r.N.Equals(q, StringComparison.OrdinalIgnoreCase) ? 0
                        : r.N.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(r => r.N.Length)
            .Take(MaxResults)
            .Select(r => new NutritionSearchHit(r.Id, r.N))
            .ToList();
    }

    public async Task<NutritionLookupResult?> GetAsync(
        string externalId, CancellationToken ct = default)
    {
        var rows = await EnsureLoadedAsync(ct);
        var row = rows.FirstOrDefault(r => r.Id == externalId);

        if (row is null) return null;

        return new NutritionLookupResult(
            ExternalId: row.Id,
            Name: row.N,
            Category: ParseCategory(row.Cat),
            CaloriesPer100g: row.Kcal,
            ProteinPer100g: row.P,
            FatPer100g: row.F,
            CarbsPer100g: row.C,
            FiberPer100g: row.Fi,
            SaltPer100g: row.S);
    }

    private static FoodCategory ParseCategory(string? value) =>
        Enum.TryParse<FoodCategory>(value, ignoreCase: true, out var cat)
            ? cat
            : FoodCategory.Other;

    // Filen läses en gång och ligger sedan i minnet. 2 600 rader med
    // sex tal vardera är försumbart.
    private async Task<List<FoodRow>> EnsureLoadedAsync(CancellationToken ct)
    {
        if (_rows is not null) return _rows;

        await _lock.WaitAsync(ct);
        try
        {
            if (_rows is not null) return _rows;

            Stream stream;
            try
            {
                stream = await _openFile();
            }
            catch (Exception ex)
            {
                // Att svälja detta vore förödande - man får noll träffar
                // utan att förstå varför.
                throw new InvalidOperationException(
                    "Kunde inte öppna livsmedel.json. Kontrollera att filen ligger i " +
                    "Resources/Raw och har Build Action = MauiAsset.", ex);
            }

            await using (stream)
            {
                var rows = await JsonSerializer.DeserializeAsync<List<FoodRow>>(
                    stream, JsonOptions, ct);

                if (rows is null || rows.Count == 0)
                    throw new InvalidOperationException(
                        "livsmedel.json lästes men innehöll inga poster.");

                _rows = rows;
            }

            return _rows;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // Korta fältnamn för att hålla filen liten. null betyder att värdet
    // saknas i databasen - inte att det är noll.
    private class FoodRow
    {
        public string Id { get; set; } = "";
        public string N { get; set; } = "";
        public string? Cat { get; set; }
        public double? Kcal { get; set; }
        public double? P { get; set; }
        public double? F { get; set; }
        public double? C { get; set; }
        public double? Fi { get; set; }
        public double? S { get; set; }
    }
}