namespace Domain.Entities;

public class RecipeIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }

    public Guid FoodId { get; set; }
    public FoodItem? Food { get; set; }

    public int WeightInGrams { get; set; }

    public int CalculatedCalories =>
        Food is null ? 0 : (int)Math.Round((double)Food.CaloriesPer100g * WeightInGrams / 100);
}