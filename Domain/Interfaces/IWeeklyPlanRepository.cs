using Domain.Entities;

namespace Domain.Interfaces;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan?> GetWeekAsync(int year, int weekNumber);
    Task SaveWeekAsync(WeeklyPlan weeklyPlan);
    Task<IEnumerable<WeeklyPlan>> GetAllWeeksAsync();

    Task AddMealAsync(Meal meal);
    Task RemoveMealAsync(Guid mealId);
}