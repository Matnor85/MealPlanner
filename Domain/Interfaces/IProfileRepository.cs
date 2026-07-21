using Domain.Entities;

namespace Domain.Interfaces;

public interface IProfileRepository
{
    Task<UserProfile?> GetAsync();
    Task SaveAsync(UserProfile profile);

    Task<IEnumerable<WeighIn>> GetWeighInsAsync();
    Task AddOrUpdateWeighInAsync(DateOnly date, double weightKg, string? note);
    Task DeleteWeighInAsync(Guid id);
}