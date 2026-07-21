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

        var extras = await context.ShoppingExtras
            .Where(e => e.Year == year && e.WeekNumber == weekNumber)
            .ToListAsync();

        foreach (var extra in extras)
            extra.IsChecked = false;

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ShoppingExtraItem>> GetExtrasAsync(int year, int weekNumber)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.ShoppingExtras
            .AsNoTracking()
            .Where(e => e.Year == year && e.WeekNumber == weekNumber)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task AddExtraAsync(int year, int weekNumber, string text)
    {
        await using var context = await _factory.CreateDbContextAsync();

        context.ShoppingExtras.Add(new ShoppingExtraItem
        {
            Year = year,
            WeekNumber = weekNumber,
            Text = text.Trim()
        });

        await context.SaveChangesAsync();
    }

    public async Task SetExtraCheckedAsync(Guid id, bool isChecked)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var extra = await context.ShoppingExtras.FirstOrDefaultAsync(e => e.Id == id);
        if (extra is null) return;

        extra.IsChecked = isChecked;
        await context.SaveChangesAsync();
    }

    public async Task RemoveExtraAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var extra = await context.ShoppingExtras.FirstOrDefaultAsync(e => e.Id == id);
        if (extra is not null)
        {
            context.ShoppingExtras.Remove(extra);
            await context.SaveChangesAsync();
        }
    }
}