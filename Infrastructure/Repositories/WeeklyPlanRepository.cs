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
            .Include(w => w.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Recipe!)
                        .ThenInclude(r => r.Ingredients)
                            .ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(w => w.Year == year && w.WeekNumber == weekNumber);

        // Databasen lovar ingen ordning på barnlistor - sortera själv
        if (week is not null)
            week.Days = week.Days.OrderBy(d => d.SortOrder).ToList();

        return week;
    }

    public async Task SaveWeekAsync(WeeklyPlan weeklyPlan)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var existing = await context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meals)
            .FirstOrDefaultAsync(w => w.Year == weeklyPlan.Year
                                   && w.WeekNumber == weeklyPlan.WeekNumber);

        if (existing is not null)
        {
            // Ta bort den gamla veckan helt - kaskaden städar dagar och måltider.
            // Enklare och säkrare än att diffa två objektträd med Update(),
            // som markerar även nya barn som Modified och kastar
            // DbUpdateConcurrencyException när raderna inte finns.
            context.WeeklyPlans.Remove(existing);
            await context.SaveChangesAsync();
        }

        context.WeeklyPlans.Add(weeklyPlan);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WeeklyPlan>> GetAllWeeksAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Food)
            .Include(w => w.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Recipe!)
                        .ThenInclude(r => r.Ingredients)
                            .ThenInclude(i => i.Food)
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