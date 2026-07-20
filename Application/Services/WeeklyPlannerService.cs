using System.Globalization;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class WeeklyPlannerService
{
    private readonly IWeeklyPlanRepository _repository;

    public WeeklyPlannerService(IWeeklyPlanRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // Måndag först, vilket är det man förväntar sig i Sverige
    private static readonly DayOfWeek[] WeekOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public async Task<WeeklyPlan> GetOrCreateWeekAsync(int year, int weekNumber)
    {
        var existingWeek = await _repository.GetWeekAsync(year, weekNumber);
        if (existingWeek != null)
            return existingWeek;

        var newWeek = new WeeklyPlan { Year = year, WeekNumber = weekNumber };

        for (int i = 0; i < WeekOrder.Length; i++)
        {
            newWeek.Days.Add(new DailyPlan
            {
                Day = WeekOrder[i],
                SortOrder = i,
                WeeklyPlanId = newWeek.Id
            });
        }

        await _repository.SaveWeekAsync(newWeek);

        return newWeek;
    }

    public async Task<WeeklyPlan> CopyWeekToNextAsync(int fromYear, int fromWeekNumber)
    {
        var sourceWeek = await _repository.GetWeekAsync(fromYear, fromWeekNumber);
        if (sourceWeek == null)
            throw new InvalidOperationException("Källveckan finns inte.");

        // ISO-år har ibland 53 veckor, så 52 duger inte som gräns
        int weeksThisYear = ISOWeek.GetWeeksInYear(sourceWeek.Year);
        bool rollover = sourceWeek.WeekNumber >= weeksThisYear;

        int targetWeek = rollover ? 1 : sourceWeek.WeekNumber + 1;
        int targetYear = rollover ? sourceWeek.Year + 1 : sourceWeek.Year;

        var newWeek = new WeeklyPlan { Year = targetYear, WeekNumber = targetWeek };

        foreach (var sourceDay in sourceWeek.Days.OrderBy(d => d.SortOrder))
        {
            var newDay = new DailyPlan
            {
                Day = sourceDay.Day,
                SortOrder = sourceDay.SortOrder,
                WeeklyPlanId = newWeek.Id
            };

            foreach (var sourceMeal in sourceDay.Meals)
            {
                // Kopiera BÅDA varianterna. Utelämnas RecipeId blir en receptmåltid
                // en måltid utan vare sig råvara eller recept, och databasens
                // check constraint stoppar hela sparningen.
                newDay.Meals.Add(new Meal
                {
                    DailyPlanId = newDay.Id,
                    Type = sourceMeal.Type,
                    FoodId = sourceMeal.FoodId,
                    WeightInGrams = sourceMeal.WeightInGrams,
                    RecipeId = sourceMeal.RecipeId,
                    Portions = sourceMeal.Portions
                });
            }

            newWeek.Days.Add(newDay);
        }

        await _repository.SaveWeekAsync(newWeek);
        return newWeek;
    }

    // Rullande veckor: skapar de N kommande veckorna om de saknas
    public async Task<List<WeeklyPlan>> EnsureUpcomingWeeksAsync(int startYear, int startWeek, int count)
    {
        var result = new List<WeeklyPlan>();
        int year = startYear, week = startWeek;

        for (int i = 0; i < count; i++)
        {
            result.Add(await GetOrCreateWeekAsync(year, week));

            if (week >= ISOWeek.GetWeeksInYear(year)) { week = 1; year++; }
            else week++;
        }

        return result;
    }

    public async Task AddMealAsync(Guid dailyPlanId, MealType type, Guid foodId, int grams)
    {
        if (grams <= 0)
            throw new ArgumentException("Vikten måste vara större än noll.", nameof(grams));

        var meal = new Meal
        {
            DailyPlanId = dailyPlanId,
            Type = type,
            FoodId = foodId,
            WeightInGrams = grams
        };

        if (!meal.IsValid)
            throw new InvalidOperationException("En måltid måste ha antingen en råvara eller ett recept.");

        await _repository.AddMealAsync(meal);
    }

    public async Task AddRecipeMealAsync(Guid dailyPlanId, MealType type, Guid recipeId, double portions)
    {
        if (portions <= 0)
            throw new ArgumentException("Antalet portioner måste vara större än noll.", nameof(portions));

        var meal = new Meal
        {
            DailyPlanId = dailyPlanId,
            Type = type,
            RecipeId = recipeId,
            Portions = portions
        };

        if (!meal.IsValid)
            throw new InvalidOperationException("En måltid måste ha antingen en råvara eller ett recept.");

        await _repository.AddMealAsync(meal);
    }

    public Task RemoveMealAsync(Guid mealId) => _repository.RemoveMealAsync(mealId);
}