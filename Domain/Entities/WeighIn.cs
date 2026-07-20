namespace Domain.Entities;

public class WeighIn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public double WeightKg { get; set; }
    public string? Note { get; set; }
}