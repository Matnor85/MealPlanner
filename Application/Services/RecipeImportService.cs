using System.Globalization;
using System.Text;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

// Ett förslag på vilken vara en tolkad ingrediens motsvarar.
public record MatchCandidate(
    Guid? OwnFoodId,
    string? ExternalId,
    string Name,
    int Score);

// En ingrediensrad i granskningsvyn. Muterbar, eftersom användaren
// justerar den innan receptet sparas.
public class IngredientDraft
{
    public string RawName { get; set; } = "";
    public double Grams { get; set; }
    public string AmountText { get; set; } = "";

    // Vald matchning. Exakt en av dessa är satt när raden är matchad.
    public Guid? ExistingFoodId { get; set; }
    public string? LookupId { get; set; }
    public string? MatchedName { get; set; }

    // Andra rimliga kandidater, ifall automatiken valde fel
    public List<MatchCandidate> Alternatives { get; set; } = new();

    public bool Include { get; set; } = true;

    // Låg poäng betyder att matchningen är en gissning värd att granska
    public bool IsUncertain { get; set; }

    public bool IsMatched => ExistingFoodId.HasValue || LookupId is not null;

    public string MatchLabel => MatchedName ?? "Ingen matchning";

    public string MatchSource => ExistingFoodId.HasValue
        ? "finns redan"
        : LookupId is not null ? "skapas ny" : "saknas";

    public void Apply(MatchCandidate candidate)
    {
        ExistingFoodId = candidate.OwnFoodId;
        LookupId = candidate.ExternalId;
        MatchedName = candidate.Name;
        IsUncertain = false;
        Include = true;
    }

    public void Clear()
    {
        ExistingFoodId = null;
        LookupId = null;
        MatchedName = null;
        Include = false;
    }
}

public class RecipeImportDraft
{
    public string Name { get; set; } = "";
    public int Portions { get; set; } = 4;
    public string? Note { get; set; }
    public List<IngredientDraft> Ingredients { get; set; } = new();
    public List<ParsedStep> Steps { get; set; } = new();

    public int MatchedCount => Ingredients.Count(i => i.IsMatched);
    public int UnmatchedCount => Ingredients.Count(i => !i.IsMatched);
    public int UncertainCount => Ingredients.Count(i => i.IsUncertain);
}

public class RecipeImportService
{
    // Under detta betraktas matchningen som en gissning och flaggas
    private const int UncertainBelow = 300;

    // Under detta räknas det inte som matchning alls
    private const int RejectBelow = 120;

    private const int MaxAlternatives = 6;

    private readonly IRecipeImporter _importer;
    private readonly INutritionLookup _lookup;
    private readonly IFoodItemRepository _foods;
    private readonly IRecipeRepository _recipes;

    public RecipeImportService(
        IRecipeImporter importer,
        INutritionLookup lookup,
        IFoodItemRepository foods,
        IRecipeRepository recipes)
    {
        _importer = importer;
        _lookup = lookup;
        _foods = foods;
        _recipes = recipes;
    }

    public bool IsConfigured => _importer.IsConfigured;

    public async Task<RecipeImportDraft> FromImageAsync(
        byte[] bytes, string mimeType, CancellationToken ct = default)
        => await BuildDraftAsync(await _importer.FromImageAsync(bytes, mimeType, ct), ct);

    public async Task<RecipeImportDraft> FromTextAsync(
        string text, CancellationToken ct = default)
        => await BuildDraftAsync(await _importer.FromTextAsync(text, ct), ct);

    private async Task<RecipeImportDraft> BuildDraftAsync(ParsedRecipe parsed, CancellationToken ct)
    {
        var ownFoods = (await _foods.GetAllAsync()).ToList();
        var lookupEntries = await _lookup.GetAllAsync(ct);

        var draft = new RecipeImportDraft
        {
            Name = parsed.Name,
            Portions = parsed.Portions,
            Note = parsed.Note,
            Steps = parsed.Steps
        };

        foreach (var ing in parsed.Ingredients)
        {
            var row = new IngredientDraft
            {
                RawName = ing.Name,
                Grams = ing.EstimatedGrams ?? 0,
                AmountText = $"{ing.Amount.ToString("0.##", CultureInfo.CurrentCulture)} {ing.UnitText}"
            };

            // Egna råvaror poängsätts med en bonus - har man redan lagt in
            // "Olivolja" ska den vinna över Livsmedelsverkets varianter.
            var candidates = ownFoods
                .Select(f => new MatchCandidate(f.Id, null, f.Name, Score(ing.Name, f.Name) + 150))
                .Concat(lookupEntries
                    .Select(e => new MatchCandidate(null, e.ExternalId, e.Name, Score(ing.Name, e.Name))))
                .Where(c => c.Score >= RejectBelow)
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.Name.Length)
                .Take(MaxAlternatives)
                .ToList();

            row.Alternatives = candidates;

            var best = candidates.FirstOrDefault();
            if (best is not null)
            {
                row.Apply(best);
                row.IsUncertain = best.Score < UncertainBelow;
            }
            else
            {
                row.Include = false;
            }

            // Saknas gramuppskattning helt, räkna om från råvarans egen enhet
            if (row.Grams <= 0 && ing.Amount > 0 && row.ExistingFoodId is Guid ownId)
            {
                var food = ownFoods.First(f => f.Id == ownId);
                row.Grams = food.ToGrams(ing.Amount, ParseUnit(ing.UnitText));
            }

            draft.Ingredients.Add(row);
        }

        return draft;
    }

    // ----- Matchning -----

    // Ordbaserad poängsättning. Ren delsträngsmatchning ger fel svar:
    // "vatten" finns i "Vattenmelon" utan att ha något med saken att göra.
    private static int Score(string query, string candidate)
    {
        string q = Fold(query);
        string c = Fold(candidate);

        if (q.Length == 0 || c.Length == 0) return 0;
        if (q == c) return 1000;

        var qWords = Words(q);
        var cWords = Words(c);

        if (qWords.Count == 0 || cWords.Count == 0) return 0;

        int score = 0;

        // Hela söksträngen förekommer som egna ord i kandidaten
        if (cWords.Contains(q)) score += 400;

        // Huvudordet står sist på svenska: "gul lök" -> lök, "ljummet vatten" -> vatten
        string head = qWords[^1];
        if (cWords.Contains(head)) score += 300;
        if (cWords[0] == head) score += 120;

        // Andel av sökorden som finns som hela ord
        int matched = qWords.Count(w => cWords.Contains(w));
        score += 150 * matched / qWords.Count;

        // Delsträng ger något, men medvetet lite - det är just den
        // regeln som gjorde vatten till vattenmelon.
        if (c.Contains(q)) score += 60;

        // Korta namn föredras: "Vatten" före "Vatten kokt m. salt"
        score -= Math.Min(120, Math.Abs(c.Length - q.Length) * 2);

        return Math.Max(0, score);
    }

    private static string Fold(string s)
    {
        var sb = new StringBuilder(s.Length);

        foreach (char ch in s.ToLowerInvariant())
        {
            sb.Append(ch switch
            {
                'å' or 'ä' => 'a',
                'ö' => 'o',
                'é' => 'e',
                _ => ch
            });
        }

        return sb.ToString().Trim();
    }

    private static readonly char[] Separators = { ' ', ',', '.', '-', '/', '(', ')' };

    private static List<string> Words(string s) =>
        s.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
         .Where(w => w.Length > 1)
         .ToList();

    private static MeasureUnit ParseUnit(string text) => text.Trim().ToLowerInvariant() switch
    {
        "st" or "stycken" or "styck" => MeasureUnit.Piece,
        "dl" or "deciliter" => MeasureUnit.Deciliter,
        "ml" or "milliliter" => MeasureUnit.Milliliter,
        "msk" or "matsked" => MeasureUnit.Tablespoon,
        "tsk" or "tesked" => MeasureUnit.Teaspoon,
        "krm" or "kryddmått" => MeasureUnit.Pinch,
        _ => MeasureUnit.Gram
    };

    // ----- Sparande -----

    // Skapar saknade råvaror, sparar receptet, och returnerar dess id.
    public async Task<Guid> SaveAsync(RecipeImportDraft draft, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draft.Name))
            throw new ArgumentException("Receptet måste ha ett namn.");

        var recipe = new Recipe
        {
            Name = draft.Name.Trim(),
            Portions = Math.Max(1, draft.Portions),
            Instructions = draft.Note
        };

        foreach (var row in draft.Ingredients.Where(i => i.Include && i.IsMatched))
        {
            Guid foodId;

            if (row.ExistingFoodId is Guid id)
            {
                foodId = id;
            }
            else
            {
                // Ny råvara hämtas från Livsmedelsverket och sparas i biblioteket
                var nutrition = await _lookup.GetAsync(row.LookupId!, ct);
                if (nutrition is null) continue;

                var food = new FoodItem
                {
                    Name = nutrition.Name,
                    Category = nutrition.Category,
                    CaloriesPer100g = (int)Math.Round(nutrition.CaloriesPer100g ?? 0),
                    ProteinPer100g = nutrition.ProteinPer100g ?? 0,
                    FatPer100g = nutrition.FatPer100g ?? 0,
                    CarbsPer100g = nutrition.CarbsPer100g ?? 0,
                    FiberPer100g = nutrition.FiberPer100g ?? 0,
                    SaltPer100g = nutrition.SaltPer100g ?? 0
                };

                await _foods.AddAsync(food);
                foodId = food.Id;
            }

            recipe.Ingredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                FoodId = foodId,
                WeightInGrams = (int)Math.Round(Math.Max(0, row.Grams))
            });
        }

        int number = 1;
        foreach (var step in draft.Steps.OrderBy(s => s.Number))
        {
            recipe.Steps.Add(new RecipeStep
            {
                RecipeId = recipe.Id,
                StepNumber = number++,
                Text = step.Text,
                Minutes = step.Minutes
            });
        }

        await _recipes.AddAsync(recipe);
        return recipe.Id;
    }
}