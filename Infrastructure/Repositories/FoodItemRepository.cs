using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class FoodItemRepository : IFoodItemRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public FoodItemRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IEnumerable<FoodItem>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.FoodItems
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<FoodItem?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.FoodItems.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task AddAsync(FoodItem food)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.FoodItems.Add(food);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Vägra radera råvaror som används i en måltid - annars spricker referensen
        bool isInUse = await context.WeeklyPlans
            .SelectMany(w => w.Days)
            .SelectMany(d => d.Meals)
            .AnyAsync(m => m.FoodId == id);

        if (isInUse)
            throw new InvalidOperationException("Råvaran används i en eller flera måltider.");

        var food = await context.FoodItems.FirstOrDefaultAsync(f => f.Id == id);
        if (food is not null)
        {
            context.FoodItems.Remove(food);
            await context.SaveChangesAsync();
        }
    }
}