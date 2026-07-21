namespace Domain.Entities;

public class RecipeIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }

    public Guid FoodId { get; set; }
    public FoodItem? Food { get; set; }

    // Lagras alltid i gram, oavsett vilken enhet råvaran matas in med
    public int WeightInGrams { get; set; }

    public Nutrition Nutrition => Food?.ForGrams(WeightInGrams) ?? Nutrition.Zero;

    public int CalculatedCalories => Nutrition.Calories;

    public string DisplayAmount => Food?.FormatAmount(WeightInGrams) ?? $"{WeightInGrams} g";
}