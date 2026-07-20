namespace Domain.Entities;

public class DailyPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Främmande nyckel till veckan dagen tillhör
    public Guid WeeklyPlanId { get; set; }

    public DayOfWeek Day { get; set; }

    // 0 = måndag ... 6 = söndag. Behövs för att kunna sortera dagarna,
    // eftersom DayOfWeek.Sunday = 0 i .NET och databasen inte lovar någon ordning.
    public int SortOrder { get; set; }

    public List<Meal> Meals { get; set; } = new();

    public int TotalCalories => Meals.Sum(m => m.CalculatedCalories);
}