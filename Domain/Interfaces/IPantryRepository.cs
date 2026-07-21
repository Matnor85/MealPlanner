using Domain.Entities;

namespace Domain.Interfaces;

public interface IPantryRepository
{
    Task<IEnumerable<PantryItem>> GetAllAsync();
    Task<PantryItem?> GetByFoodAsync(Guid foodId);

    // Lägger till mängd på en befintlig rad, eller skapar en ny
    Task AddAmountAsync(Guid foodId, double grams, DateOnly? bestBefore);

    // Sätter mängden till ett exakt värde
    Task SetAmountAsync(Guid foodId, double grams);

    Task SetBestBeforeAsync(Guid foodId, DateOnly? bestBefore);
    Task RemoveAsync(Guid foodId);
    Task ClearAsync();
}