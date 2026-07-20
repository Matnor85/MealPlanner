using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RecipeRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IEnumerable<Recipe>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
            .Include(r => r.Steps)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Recipe?> GetByIdAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var recipe = await context.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe is not null)
            recipe.Steps = recipe.Steps.OrderBy(s => s.StepNumber).ToList();

        return recipe;
    }

    public async Task AddAsync(Recipe recipe)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Recipe recipe)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var existing = await context.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id);
        if (existing is null) return;

        existing.Name = recipe.Name;
        existing.Portions = recipe.Portions;
        existing.Instructions = recipe.Instructions;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        bool isInUse = await context.WeeklyPlans
            .SelectMany(w => w.Days)
            .SelectMany(d => d.Meals)
            .AnyAsync(m => m.RecipeId == id);

        if (isInUse)
            throw new InvalidOperationException("Receptet används i en eller flera måltider.");

        var recipe = await context.Recipes.FirstOrDefaultAsync(r => r.Id == id);
        if (recipe is not null)
        {
            context.Recipes.Remove(recipe);
            await context.SaveChangesAsync();
        }
    }

    public async Task AddIngredientAsync(RecipeIngredient ingredient)
    {
        await using var context = await _factory.CreateDbContextAsync();
        context.Set<RecipeIngredient>().Add(ingredient);
        await context.SaveChangesAsync();
    }

    public async Task RemoveIngredientAsync(Guid ingredientId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var ing = await context.Set<RecipeIngredient>().FirstOrDefaultAsync(i => i.Id == ingredientId);
        if (ing is not null)
        {
            context.Set<RecipeIngredient>().Remove(ing);
            await context.SaveChangesAsync();
        }
    }
}