namespace Domain.Entities;

// Fritextrader på inköpslistan - diskmedel, hushållspapper, sådant som
// inte är en FoodItem och aldrig kommer från veckoplanen.
public class ShoppingExtraItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Year { get; set; }
    public int WeekNumber { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}