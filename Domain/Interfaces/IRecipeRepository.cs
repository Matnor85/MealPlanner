using Domain.Entities;

namespace Domain.Interfaces;

public interface IRecipeRepository
{
    Task<IEnumerable<Recipe>> GetAllAsync();
    Task<Recipe?> GetByIdAsync(Guid id);
    Task AddAsync(Recipe recipe);
    Task UpdateAsync(Recipe recipe);
    Task DeleteAsync(Guid id);

    Task AddIngredientAsync(RecipeIngredient ingredient);
    Task RemoveIngredientAsync(Guid ingredientId);
}