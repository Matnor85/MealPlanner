using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class PantryService
{
    private readonly IPantryRepository _pantry;

    public PantryService(IPantryRepository pantry) => _pantry = pantry;

    public async Task<List<PantryItem>> GetAllAsync()
    {
        var items = (await _pantry.GetAllAsync()).ToList();

        // Mest brådskande först, därefter alfabetiskt. Poängen är att det
        // som håller på att gå ut syns utan att man letar.
        return items
            .OrderByDescending(i => i.Expiry)
            .ThenBy(i => i.BestBefore ?? DateOnly.MaxValue)
            .ThenBy(i => i.Food?.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public Task AddAsync(Guid foodId, double grams, DateOnly? bestBefore = null)
    {
        if (grams <= 0)
            throw new ArgumentException("Mängden måste vara större än noll.", nameof(grams));

        return _pantry.AddAmountAsync(foodId, grams, bestBefore);
    }

    public Task SetAmountAsync(Guid foodId, double grams) => _pantry.SetAmountAsync(foodId, grams);

    public Task SetBestBeforeAsync(Guid foodId, DateOnly? date) =>
        _pantry.SetBestBeforeAsync(foodId, date);

    public Task RemoveAsync(Guid foodId) => _pantry.RemoveAsync(foodId);

    public Task ClearAsync() => _pantry.ClearAsync();

    // Hur mycket som finns hemma av varje råvara, som uppslagstabell.
    // Används av receptförslagen och inköpslistan.
    public async Task<Dictionary<Guid, double>> GetAvailableGramsAsync()
    {
        var items = await _pantry.GetAllAsync();
        return items.ToDictionary(i => i.FoodId, i => i.Grams);
    }
}