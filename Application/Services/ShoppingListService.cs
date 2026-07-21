using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

// En rad på inköpslistan. Räknas alltid fram - lagras aldrig.
public record ShoppingLine(
    FoodItem Food,
    int NeededGrams,
    int PantryGrams,
    bool IsChecked)
{
    // Vad du faktiskt måste köpa när skafferiet räknats av
    public int ToBuyGrams => Math.Max(0, NeededGrams - PantryGrams);

    public bool FullyCovered => ToBuyGrams == 0;
}

public class ShoppingListService
{
    private readonly IWeeklyPlanRepository _weeks;
    private readonly IShoppingRepository _states;
    private readonly IPantryRepository _pantry;

    public ShoppingListService(
        IWeeklyPlanRepository weeks,
        IShoppingRepository states,
        IPantryRepository pantry)
    {
        _weeks = weeks;
        _states = states;
        _pantry = pantry;
    }

    public async Task<List<ShoppingLine>> BuildAsync(int year, int weekNumber)
    {
        var week = await _weeks.GetWeekAsync(year, weekNumber);
        if (week is null) return new List<ShoppingLine>();

        // Summera gram per råvara över hela veckan
        var totals = new Dictionary<Guid, (FoodItem Food, double Grams)>();

        void Add(FoodItem? food, double grams)
        {
            if (food is null || grams <= 0) return;

            if (totals.TryGetValue(food.Id, out var current))
                totals[food.Id] = (current.Food, current.Grams + grams);
            else
                totals[food.Id] = (food, grams);
        }

        foreach (var meal in week.Days.SelectMany(d => d.Meals))
        {
            if (meal.Recipe is not null)
            {
                // Ingrediensmängderna gäller hela receptet, alltså Recipe.Portions
                // portioner. Skala ner till en portion och upp till måltidens antal.
                int recipePortions = Math.Max(1, meal.Recipe.Portions);

                foreach (var ing in meal.Recipe.Ingredients)
                    Add(ing.Food, (double)ing.WeightInGrams / recipePortions * meal.Portions);
            }
            else
            {
                Add(meal.Food, meal.WeightInGrams);
            }
        }

        var states = (await _states.GetStatesAsync(year, weekNumber))
            .ToDictionary(s => s.FoodId, s => s.IsChecked);

        var pantry = (await _pantry.GetAllAsync())
            .ToDictionary(p => p.FoodId, p => p.Grams);

        return totals.Values
            .Select(t => new ShoppingLine(
                t.Food,
                (int)Math.Round(t.Grams),
                (int)Math.Round(pantry.GetValueOrDefault(t.Food.Id, 0)),
                states.GetValueOrDefault(t.Food.Id, false)))
            .OrderBy(l => l.Food.Category)
            .ThenBy(l => l.Food.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public Task<List<ShoppingExtraItem>> GetExtrasAsync(int year, int weekNumber) =>
        _states.GetExtrasAsync(year, weekNumber).ContinueWith(t => t.Result.ToList());

    public Task AddExtraAsync(int year, int weekNumber, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Skriv något att lägga till.", nameof(text));

        return _states.AddExtraAsync(year, weekNumber, text);
    }

    public Task SetExtraCheckedAsync(Guid id, bool isChecked) =>
        _states.SetExtraCheckedAsync(id, isChecked);

    public Task RemoveExtraAsync(Guid id) => _states.RemoveExtraAsync(id);

    public Task SetCheckedAsync(int year, int weekNumber, Guid foodId, bool isChecked)
        => _states.SetCheckedAsync(year, weekNumber, foodId, isChecked);

    // Bockar av OCH lägger mängden i skafferiet. Används när användaren
    // valt att listan ska fylla på skafferiet automatiskt.
    public async Task CheckAndStockAsync(int year, int weekNumber, ShoppingLine line, bool isChecked)
    {
        await _states.SetCheckedAsync(year, weekNumber, line.Food.Id, isChecked);

        if (!isChecked || line.ToBuyGrams <= 0) return;

        await _pantry.AddAmountAsync(line.Food.Id, line.ToBuyGrams, null);
    }

    public Task ClearChecksAsync(int year, int weekNumber)
        => _states.ClearWeekAsync(year, weekNumber);
}