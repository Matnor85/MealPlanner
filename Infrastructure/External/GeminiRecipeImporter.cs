using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.External;

public class GeminiRecipeImporter : IRecipeImporter
{
    // Modellnamn ändras över tid. -latest följer med automatiskt.
    // Aktuell lista: https://ai.google.dev/gemini-api/docs/models
    private const string Model = "gemini-flash-latest";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiRecipeImporter(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    private const string Prompt = """
        Läs ut matreceptet ur bilden eller texten och svara med ENDAST ett
        JSON-objekt enligt detta schema:

        {
          "name": "receptets namn",
          "portions": 4,
          "note": "kort anteckning eller null",
          "ingredients": [
            { "name": "råvarans namn", "amount": 3, "unit": "dl", "grams": 300 }
          ],
          "steps": [
            { "number": 1, "text": "hela instruktionen", "minutes": 5 }
          ]
        }

        NAMN PÅ INGREDIENSER
        Grundregeln: skriv det du hade sökt efter i en näringsdatabas.
        Stryk allt som beskriver tillstånd, temperatur, varumärke, hantering
        eller valfrihet. Behåll det som avgör VILKEN vara det är.

        Stryk: ljummet, kallt, färsk, finhackad, riven, skalad, hackad,
        gärna, valfri, ekologisk, samt alla varumärken.
        Behåll: sorten (fullkorn, torkad, rökt, krossad), fetthalt, och ord
        som skiljer varan från en annan vara.

        Exempel på båda:
          "ljummet vatten 37 grader"          -> "vatten"
          "2 st ägg, rumsvarma"               -> "ägg"
          "Arla Köket vispgrädde 36%"         -> "vispgrädde"
          "1 näve färsk basilika, grovhackad" -> "basilika"
          "smör till stekning"                -> "smör"
          "400 g krossade tomater"            -> "krossade tomater"
          "2 dl fullkornsmjöl"                -> "fullkornsmjöl"
          "rökt sidfläsk i tärningar"         -> "rökt sidfläsk"

        MÄNGDER
        - "unit" ska vara ett av: st, g, dl, ml, msk, tsk, krm.
        - "grams" är din bästa uppskattning av mängden i gram. För vätskor
          motsvarar 1 dl ungefär 100 g; torrvaror väger mindre, fett ungefär
          lika mycket. Sätt null bara om du verkligen inte kan bedöma det.
        - Vid intervall som "6-8 dl", välj det lägre värdet.
        - Vid "efter smak", "en nypa" eller liknande utan mängd: sätt amount
          till 1, unit till "krm" och grams till null.
        - Ingredienser som bara nämns i instruktionerna, till exempel "salta
          vattnet", ska INTE tas med i ingredienslistan.

        STEG
        - Ett steg per moment, i den ordning de utförs.
        - Behåll hela instruktionstexten, inklusive temperaturer och tider.
        - "minutes" är ungefärlig tid för just det steget, eller null om
          steget inte tar nämnvärd tid.
        - Hoppa över reklamtext, tips och länkar som inte är instruktioner.

        ÖVRIGT
        - Innehåller receptet flera avdelningar, som deg och fyllning, slå
          ihop alla ingredienser till en lista och behåll stegen i ordning.
        - "portions" avser antal portioner. Står det "4 pizzor" eller
          "12 bullar", ange det antalet.
        - Saknas portionsantal helt, gissa utifrån mängderna.
        - Hitta aldrig på ingredienser eller steg som inte står i källan.
        - Svara på svenska.
        - Skriv ingenting utanför JSON-objektet.
        """;

    public async Task<ParsedRecipe> FromImageAsync(
        byte[] imageBytes, string mimeType, CancellationToken ct = default)
    {
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = Prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = Convert.ToBase64String(imageBytes)
                            }
                        }
                    }
                }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };

        return await SendAsync(payload, ct);
    }

    public async Task<ParsedRecipe> FromTextAsync(string text, CancellationToken ct = default)
    {
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = Prompt },
                        new { text = "\n\nRecept:\n" + text }
                    }
                }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };

        return await SendAsync(payload, ct);
    }

    private async Task<ParsedRecipe> SendAsync(object payload, CancellationToken ct)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Ingen API-nyckel angiven. Lägg in den i ApiKeys.Gemini.");

        string url = $"{BaseUrl}{Model}:generateContent?key={_apiKey}";

        using var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync(url, content, ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Bildtolkningen misslyckades ({(int)response.StatusCode}). {Trim(body)}");

        var envelope = JsonSerializer.Deserialize<GeminiResponse>(body, JsonOptions);

        string? json = envelope?.Candidates?.FirstOrDefault()
            ?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Tomt svar från bildtolkningen.");

        // Modellen lägger ibland ändå ```json runt svaret
        json = json.Trim().Trim('`');
        if (json.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            json = json[4..];

        var parsed = JsonSerializer.Deserialize<RecipeDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Kunde inte tolka receptet.");

        return Map(parsed);
    }

    private static ParsedRecipe Map(RecipeDto dto) => new(
        Name: string.IsNullOrWhiteSpace(dto.Name) ? "Nytt recept" : dto.Name.Trim(),
        Portions: dto.Portions is > 0 ? dto.Portions.Value : 4,
        Note: string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
        Ingredients: (dto.Ingredients ?? new())
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => new ParsedIngredient(
                i.Name!.Trim(),
                i.Amount ?? 0,
                i.Unit ?? "g",
                i.Grams))
            .ToList(),
        Steps: (dto.Steps ?? new())
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select((s, index) => new ParsedStep(
                s.Number ?? index + 1,
                s.Text!.Trim(),
                s.Minutes))
            .OrderBy(s => s.Number)
            .ToList());

    private static string Trim(string s) => s.Length > 300 ? s[..300] + "..." : s;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // ----- Svarsstruktur från Gemini -----

    private class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        public ContentDto? Content { get; set; }
    }

    private class ContentDto
    {
        public List<PartDto>? Parts { get; set; }
    }

    private class PartDto
    {
        public string? Text { get; set; }
    }

    // ----- Receptet som modellen svarar med -----

    private class RecipeDto
    {
        public string? Name { get; set; }
        public int? Portions { get; set; }
        public string? Note { get; set; }
        public List<IngredientDto>? Ingredients { get; set; }
        public List<StepDto>? Steps { get; set; }
    }

    private class IngredientDto
    {
        public string? Name { get; set; }
        public double? Amount { get; set; }
        public string? Unit { get; set; }
        public double? Grams { get; set; }
    }

    private class StepDto
    {
        public int? Number { get; set; }
        public string? Text { get; set; }
        public int? Minutes { get; set; }
    }
}