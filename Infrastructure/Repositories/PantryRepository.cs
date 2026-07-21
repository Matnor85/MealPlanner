using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PantryRepository : IPantryRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PantryRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IEnumerable<PantryItem>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.PantryItems
            .Include(p => p.Food)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PantryItem?> GetByFoodAsync(Guid foodId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.PantryItems
            .Include(p => p.Food)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.FoodId == foodId);
    }

    public async Task AddAmountAsync(Guid foodId, double grams, DateOnly? bestBefore)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var item = await context.PantryItems.FirstOrDefaultAsync(p => p.FoodId == foodId);

        if (item is null)
        {
            context.PantryItems.Add(new PantryItem
            {
                FoodId = foodId,
                Grams = Math.Max(0, grams),
                BestBefore = bestBefore
            });
        }
        else
        {
            item.Grams = Math.Max(0, item.Grams + grams);
            item.UpdatedAt = DateTime.Now;

            // Kortast hållbarhet vinner - det är den som avgör när du måste äta upp
            if (bestBefore is DateOnly incoming &&
                (item.BestBefore is null || incoming < item.BestBefore))
            {
                item.BestBefore = incoming;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task SetAmountAsync(Guid foodId, double grams)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var item = await context.PantryItems.FirstOrDefaultAsync(p => p.FoodId == foodId);
        if (item is null) return;

        if (grams <= 0)
        {
            context.PantryItems.Remove(item);
        }
        else
        {
            item.Grams = grams;
            item.UpdatedAt = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }

    public async Task SetBestBeforeAsync(Guid foodId, DateOnly? bestBefore)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var item = await context.PantryItems.FirstOrDefaultAsync(p => p.FoodId == foodId);
        if (item is null) return;

        item.BestBefore = bestBefore;
        item.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid foodId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var item = await context.PantryItems.FirstOrDefaultAsync(p => p.FoodId == foodId);
        if (item is not null)
        {
            context.PantryItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }

    public async Task ClearAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.PantryItems.RemoveRange(context.PantryItems);
        await context.SaveChangesAsync();
    }
}