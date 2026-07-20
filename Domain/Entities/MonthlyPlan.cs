namespace Domain.Entities;

public class MonthlyPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Month { get; set; }
    public int Year { get; set; }

    // Kopplingen till veckor är medvetet borttagen. En månadssammanställning
    // räknas i stället fram från veckorna när den behövs.
}