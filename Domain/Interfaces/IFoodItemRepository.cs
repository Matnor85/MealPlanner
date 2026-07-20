namespace Domain.Entities;

public interface IFoodItemRepository
{
    Task<IEnumerable<FoodItem>> GetAllAsync();
    Task<FoodItem?> GetByIdAsync(Guid id);
    Task AddAsync(FoodItem food);
    Task DeleteAsync(Guid id);
}
