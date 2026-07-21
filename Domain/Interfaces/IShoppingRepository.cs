using Domain.Entities;

namespace Domain.Interfaces;

public interface IShoppingRepository
{
    Task<IEnumerable<ShoppingItemState>> GetStatesAsync(int year, int weekNumber);
    Task SetCheckedAsync(int year, int weekNumber, Guid foodId, bool isChecked);
    Task ClearWeekAsync(int year, int weekNumber);

    Task<IEnumerable<ShoppingExtraItem>> GetExtrasAsync(int year, int weekNumber);
    Task AddExtraAsync(int year, int weekNumber, string text);
    Task SetExtraCheckedAsync(Guid id, bool isChecked);
    Task RemoveExtraAsync(Guid id);
}