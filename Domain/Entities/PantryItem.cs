namespace Domain.Entities;

// Vad som faktiskt finns hemma. En rad per råvara - mängder slås ihop.
public class PantryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FoodId { get; set; }
    public FoodItem? Food { get; set; }

    // Lagras i gram, precis som allt annat
    public double Grams { get; set; }

    // Valfritt bäst före-datum
    public DateOnly? BestBefore { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string DisplayAmount => Food?.FormatAmount(Grams) ?? $"{Math.Round(Grams)} g";

    public int? DaysLeft => BestBefore is DateOnly d
        ? d.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber
        : null;

    public bool IsExpired => DaysLeft is int d && d < 0;
    public bool IsExpiringSoon => DaysLeft is int d && d >= 0 && d <= 3;
}