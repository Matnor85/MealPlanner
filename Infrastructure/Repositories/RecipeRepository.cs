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

    public async Task AddStepAsync(RecipeStep step)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Nya steg hamnar sist
        int next = await context.Set<RecipeStep>()
            .Where(s => s.RecipeId == step.RecipeId)
            .Select(s => (int?)s.StepNumber)
            .MaxAsync() ?? 0;

        step.StepNumber = next + 1;

        context.Set<RecipeStep>().Add(step);
        await context.SaveChangesAsync();
    }

    public async Task RemoveStepAsync(Guid stepId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var step = await context.Set<RecipeStep>().FirstOrDefaultAsync(s => s.Id == stepId);
        if (step is null) return;

        Guid recipeId = step.RecipeId;
        context.Set<RecipeStep>().Remove(step);
        await context.SaveChangesAsync();

        // Numrera om så det inte blir hål i sekvensen
        await RenumberAsync(context, recipeId);
    }

    // direction: -1 flyttar uppåt, +1 nedåt
    public async Task MoveStepAsync(Guid stepId, int direction)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var step = await context.Set<RecipeStep>().FirstOrDefaultAsync(s => s.Id == stepId);
        if (step is null) return;

        var siblings = await context.Set<RecipeStep>()
            .Where(s => s.RecipeId == step.RecipeId)
            .OrderBy(s => s.StepNumber)
            .ToListAsync();

        int index = siblings.FindIndex(s => s.Id == stepId);
        int target = index + direction;

        if (target < 0 || target >= siblings.Count) return;

        (siblings[index].StepNumber, siblings[target].StepNumber) =
            (siblings[target].StepNumber, siblings[index].StepNumber);

        await context.SaveChangesAsync();
    }

    private static async Task RenumberAsync(AppDbContext context, Guid recipeId)
    {
        var steps = await context.Set<RecipeStep>()
            .Where(s => s.RecipeId == recipeId)
            .OrderBy(s => s.StepNumber)
            .ToListAsync();

        for (int i = 0; i < steps.Count; i++)
            steps[i].StepNumber = i + 1;

        await context.SaveChangesAsync();
    }
}