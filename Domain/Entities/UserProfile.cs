namespace Domain.Entities;

public enum Sex { Female, Male, Unspecified }

public enum ActivityLevel
{
    Sedentary = 1,      // stillasittande
    Light = 2,          // lätt aktiv, 1-3 pass/vecka
    Moderate = 3,       // 3-5 pass/vecka
    Active = 4,         // 6-7 pass/vecka
    VeryActive = 5      // fysiskt arbete eller daglig hård träning
}

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Name { get; set; }
    public Sex Sex { get; set; } = Sex.Unspecified;
    public DateOnly? BirthDate { get; set; }
    public int HeightInCm { get; set; }

    public double StartWeightKg { get; set; }
    public double? GoalWeightKg { get; set; }

    public ActivityLevel Activity { get; set; } = ActivityLevel.Light;

    // Sätts av användaren. Är den null räknas ett förslag fram i stället.
    public int? DailyCalorieGoal { get; set; }

    public int Age => BirthDate is null
        ? 0
        : DateTime.Today.Year - BirthDate.Value.Year
          - (DateTime.Today.DayOfYear < BirthDate.Value.DayOfYear ? 1 : 0);
}