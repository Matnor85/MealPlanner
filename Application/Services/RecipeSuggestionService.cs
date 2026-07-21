using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public record MissingIngredient(FoodItem Food, double NeededGrams, double AvailableGrams)
{
    public double ShortfallGrams => Math.Max(0, NeededGrams - AvailableGrams);
}

public record Substitution(FoodItem Original, FoodItem Replacement, double Grams);

public record RecipeSuggestion(
    Recipe Recipe,
    double Coverage,
    List<MissingIngredient> Missing,
    List<Substitution> Substitutions,
    bool UsesExpiring)
{
    public bool CanCookNow => Missing.Count == 0;
    public bool NeedsSubstitution => Substitutions.Count > 0;
    public int CoveragePercent => (int)Math.Round(Coverage * 100);
}

public class RecipeSuggestionService
{
    private readonly IRecipeRepository _recipes;
    private readonly IPantryRepository _pantry;

    public RecipeSuggestionService(IRecipeRepository recipes, IPantryRepository pantry)
    {
        _recipes = recipes;
        _pantry = pantry;
    }

    // Basvaror som nästan alltid finns hemma. Att sakna salt ska inte
    // diskvalificera en rätt, så de väger lättare i täckningsgraden.
    private static bool IsStaple(FoodItem food) =>
        food.Category is FoodCategory.Fat or FoodCategory.Other;

    private const double StapleWeight = 0.2;

    // Ett utbyte räknas som täckt, men inte fullt ut - rätten blir en
    // annan rätt, och det ska synas i sorteringen.
    private const double SubstitutionCredit = 0.85;

    // Litet överskott godtas. 95 g mjölk när receptet vill ha 100 duger.
    private const double Tolerance = 0.95;

    public async Task<List<RecipeSuggestion>> SuggestAsync()
    {
        var pantryItems = (await _pantry.GetAllAsync()).ToList();

        // Tomt skafferi ger inga meningsfulla förslag. Bättre att svara
        // med en tom lista och låta gränssnittet förklara varför.
        if (pantryItems.Count == 0)
            return new List<RecipeSuggestion>();

        var recipes = (await _recipes.GetAllAsync()).ToList();

        var available = pantryItems.ToDictionary(p => p.FoodId, p => p.Grams);

        var pantryFoods = pantryItems
            .Where(p => p.Food is not null)
            .ToDictionary(p => p.FoodId, p => p.Food!);

        var expiringSoon = pantryItems
            .Where(p => p.IsExpiringSoon || p.IsExpired)
            .Select(p => p.FoodId)
            .ToHashSet();

        var suggestions = new List<RecipeSuggestion>();

        foreach (var recipe in recipes)
        {
            if (recipe.Ingredients.Count == 0) continue;

            double weightedTotal = 0;
            double weightedCovered = 0;
            var missing = new List<MissingIngredient>();
            var substitutions = new List<Substitution>();
            bool usesExpiring = false;

            // Hur mycket som redan bokats upp av varje råvara i det här receptet,
            // så samma linser inte kan ersätta två olika ingredienser.
            var reserved = new Dictionary<Guid, double>();

            foreach (var ing in recipe.Ingredients)
            {
                if (ing.Food is null) continue;

                double needed = ing.WeightInGrams;
                double weight = IsStaple(ing.Food) ? StapleWeight : 1.0;
                weightedTotal += weight;

                double have = available.GetValueOrDefault(ing.FoodId, 0)
                            - reserved.GetValueOrDefault(ing.FoodId, 0);

                if (needed <= 0 || have >= needed * Tolerance)
                {
                    weightedCovered += weight;
                    reserved[ing.FoodId] = reserved.GetValueOrDefault(ing.FoodId, 0) + needed;

                    if (expiringSoon.Contains(ing.FoodId))
                        usesExpiring = true;

                    continue;
                }

                // Finns ingen originalvara - kan något annat i samma grupp gå?
                var swap = FindSubstitute(ing.Food, needed, pantryFoods, available, reserved);

                if (swap is not null)
                {
                    weightedCovered += weight * SubstitutionCredit;
                    reserved[swap.Id] = reserved.GetValueOrDefault(swap.Id, 0) + needed;
                    substitutions.Add(new Substitution(ing.Food, swap, needed));

                    if (expiringSoon.Contains(swap.Id))
                        usesExpiring = true;

                    continue;
                }

                // Delvis täckning räknas proportionellt
                weightedCovered += weight * Math.Min(1.0, Math.Max(0, have) / needed);
                missing.Add(new MissingIngredient(ing.Food, needed, Math.Max(0, have)));
            }

            double coverage = weightedTotal <= 0 ? 0 : weightedCovered / weightedTotal;

            suggestions.Add(new RecipeSuggestion(
                recipe, coverage, missing, substitutions, usesExpiring));
        }

        // Recept som räddar något som håller på att gå ut hamnar överst,
        // därefter de du kan laga direkt utan byten, sedan efter täckningsgrad.
        return suggestions
            .OrderByDescending(s => s.UsesExpiring && s.CanCookNow)
            .ThenByDescending(s => s.CanCookNow && !s.NeedsSubstitution)
            .ThenByDescending(s => s.CanCookNow)
            .ThenByDescending(s => s.UsesExpiring)
            .ThenByDescending(s => s.Coverage)
            .ThenBy(s => s.Recipe.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    // Letar en råvara i skafferiet ur samma utbytesgrupp som täcker hela behovet.
    private static FoodItem? FindSubstitute(
        FoodItem original,
        double needed,
        Dictionary<Guid, FoodItem> pantryFoods,
        Dictionary<Guid, double> available,
        Dictionary<Guid, double> reserved)
    {
        if (original.Substitutes == SubstitutionGroup.None) return null;

        return pantryFoods.Values
            .Where(f => f.Id != original.Id
                     && f.Substitutes == original.Substitutes
                     && available.GetValueOrDefault(f.Id, 0)
                        - reserved.GetValueOrDefault(f.Id, 0) >= needed * Tolerance)
            // Ta den med minst överskott, så den stora förpackningen sparas
            .OrderBy(f => available.GetValueOrDefault(f.Id, 0))
            .FirstOrDefault();
    }
}