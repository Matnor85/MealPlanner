namespace Domain.Entities;

// Sparar INTE inköpslistan, bara vad användaren bockat av.
// Själva listan räknas alltid fram från veckans måltider.
public class ShoppingItemState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public Guid FoodId { get; set; }
    public bool IsChecked { get; set; }
}