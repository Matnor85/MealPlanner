namespace Domain.Entities;

public class Recipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    // Hur många portioner hela receptet ger
    public int Portions { get; set; } = 4;

    public List<RecipeIngredient> Ingredients { get; set; } = new();

    public int TotalCalories => Ingredients.Sum(i => i.CalculatedCalories);
    public int CaloriesPerPortion => Portions <= 0 ? 0 : TotalCalories / Portions;
}