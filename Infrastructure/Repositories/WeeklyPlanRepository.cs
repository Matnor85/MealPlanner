using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WeeklyPlanRepository : IWeeklyPlanRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public WeeklyPlanRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<WeeklyPlan?> GetWeekAsync(int year, int weekNumber)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var week = await context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Food)
            .FirstOrDefaultAsync(w => w.Year == year && w.WeekNumber == weekNumber);

        // Databasen lovar ingen ordning på barnlistor - sortera själv
        if (week is not null)
            week.Days = week.Days.OrderBy(d => d.SortOrder).ToList();

        return week;
    }

    public async Task SaveWeekAsync(WeeklyPlan weeklyPlan)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Matcha på år + veckonummer, inte på Id. Annars skapas dubbletter
        // när en ny WeeklyPlan-instans får ett färskt Guid.
        var existingId = await context.WeeklyPlans
            .AsNoTracking()
            .Where(w => w.Year == weeklyPlan.Year && w.WeekNumber == weeklyPlan.WeekNumber)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync();

        if (existingId is null)
        {
            context.WeeklyPlans.Add(weeklyPlan);
        }
        else
        {
            weeklyPlan.Id = existingId.Value;
            context.WeeklyPlans.Update(weeklyPlan);
        }

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WeeklyPlan>> GetAllWeeksAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.WeeklyPlans
            .Include(w => w.Days)
            .OrderBy(w => w.Year)
            .ThenBy(w => w.WeekNumber)
            .ToListAsync();
    }

    public async Task AddMealAsync(Meal meal)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Set<Meal>().Add(meal);
        await context.SaveChangesAsync();
    }

    public async Task RemoveMealAsync(Guid mealId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var meal = await context.Set<Meal>().FirstOrDefaultAsync(m => m.Id == mealId);
        if (meal is not null)
        {
            context.Set<Meal>().Remove(meal);
            await context.SaveChangesAsync();
        }
    }
}