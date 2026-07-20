using Domain.Entities;

public class Meal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MealType Type { get; set; } = MealType.Snack;

    // Främmande nyckel — pekar ut vilken råvara måltiden använder
    public Guid? FoodId { get; set; }
    public FoodItem? Food { get; set; }

    public int WeightInGrams { get; set; }

    public int CalculatedCalories =>
        Food is null ? 0 : (int)Math.Round((double)Food.CaloriesPer100g * WeightInGrams / 100);

    public bool ContainsLactose => Food?.Lactose ?? false;
    public bool ContainsGluten => Food?.Gluten ?? false;
    public bool IsVegan => Food?.Vegan ?? false;
    public bool ContainsMilkPowder => Food?.MilkPowder ?? false;
    public bool ContainsNuts => Food?.Nuts ?? false;
}