using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

// En rad på inköpslistan. Räknas alltid fram - lagras aldrig.
public record ShoppingLine(FoodItem Food, int TotalGrams, bool IsChecked);

public class ShoppingListService
{
    private readonly IWeeklyPlanRepository _weeks;
    private readonly IShoppingRepository _states;

    public ShoppingListService(IWeeklyPlanRepository weeks, IShoppingRepository states)
    {
        _weeks = weeks;
        _states = states;
    }

    public async Task<List<ShoppingLine>> BuildAsync(int year, int weekNumber)
    {
        var week = await _weeks.GetWeekAsync(year, weekNumber);
        if (week is null) return new List<ShoppingLine>();

        // Summera gram per råvara över hela veckan
        var totals = new Dictionary<Guid, (FoodItem Food, int Grams)>();

        void Add(FoodItem? food, double grams)
        {
            if (food is null || grams <= 0) return;

            if (totals.TryGetValue(food.Id, out var current))
                totals[food.Id] = (current.Food, current.Grams + (int)Math.Round(grams));
            else
                totals[food.Id] = (food, (int)Math.Round(grams));
        }

        foreach (var meal in week.Days.SelectMany(d => d.Meals))
        {
            if (meal.Recipe is not null)
            {
                // Ingrediensmängderna gäller hela receptet, alltså Recipe.Portions portioner.
                // Skala ner till en portion och upp till antalet portioner i måltiden.
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

        return totals.Values
            .Select(t => new ShoppingLine(
                t.Food,
                t.Grams,
                states.TryGetValue(t.Food.Id, out bool c) && c))
            .OrderBy(l => l.Food.Category)
            .ThenBy(l => l.Food.Name)
            .ToList();
    }

    public Task SetCheckedAsync(int year, int weekNumber, Guid foodId, bool isChecked)
        => _states.SetCheckedAsync(year, weekNumber, foodId, isChecked);

    public Task ClearChecksAsync(int year, int weekNumber)
        => _states.ClearWeekAsync(year, weekNumber);
}