namespace Domain.Entities;

public class WeeklyPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Year { get; set; }
    public int WeekNumber { get; set; }

    public List<DailyPlan> Days { get; set; } = new();

    public Nutrition Nutrition => Nutrition.Sum(Days.Select(d => d.Nutrition)).Rounded();

    public int TotalWeeklyCalories => Nutrition.Calories;
}