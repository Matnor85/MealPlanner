using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ShoppingRepository : IShoppingRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ShoppingRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IEnumerable<ShoppingItemState>> GetStatesAsync(int year, int weekNumber)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.ShoppingItems
            .AsNoTracking()
            .Where(s => s.Year == year && s.WeekNumber == weekNumber)
            .ToListAsync();
    }

    public async Task SetCheckedAsync(int year, int weekNumber, Guid foodId, bool isChecked)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var state = await context.ShoppingItems
            .FirstOrDefaultAsync(s => s.Year == year && s.WeekNumber == weekNumber && s.FoodId == foodId);

        if (state is null)
        {
            context.ShoppingItems.Add(new ShoppingItemState
            {
                Year = year,
                WeekNumber = weekNumber,
                FoodId = foodId,
                IsChecked = isChecked
            });
        }
        else
        {
            state.IsChecked = isChecked;
        }

        await context.SaveChangesAsync();
    }

    public async Task ClearWeekAsync(int year, int weekNumber)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var states = await context.ShoppingItems
            .Where(s => s.Year == year && s.WeekNumber == weekNumber)
            .ToListAsync();

        context.ShoppingItems.RemoveRange(states);
        await context.SaveChangesAsync();
    }
}