namespace Domain.Entities;

public class Meal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Främmande nyckel till dagen måltiden tillhör
    public Guid DailyPlanId { get; set; }

    // Standard är mellanmål om inget väljs
    public MealType Type { get; set; } = MealType.Snack;

    // Främmande nyckel till råvaran. Nullable = måltiden kan finnas
    // innan användaren hunnit välja vad som ska ätas.
    public Guid? FoodId { get; set; }
    public FoodItem? Food { get; set; }

    public int WeightInGrams { get; set; }

    // Antingen FoodId ELLER RecipeId är satt - aldrig båda
    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Antal portioner när måltiden är ett recept
    public double Portions { get; set; } = 1;

    public int CalculatedCalories
    {
        get
        {
            if (Recipe is not null)
                return (int)Math.Round(Recipe.CaloriesPerPortion * Portions);
            if (Food is not null)
                return (int)Math.Round((double)Food.CaloriesPer100g * WeightInGrams / 100);
            return 0;
        }
    }

    public bool ContainsLactose => Recipe is not null
        ? Recipe.Ingredients.Any(i => i.Food?.Lactose ?? false)
        : Food?.Lactose ?? false;
    public bool ContainsGluten => Recipe is not null
        ? Recipe.Ingredients.Any(i => i.Food?.Gluten ?? false)
        : Food?.Gluten ?? false;
    public bool IsVegan => Recipe is not null
        ? Recipe.Ingredients.All(i => i.Food?.Vegan ?? false)
        : Food?.Vegan ?? false;
    public bool ContainsMilkPowder => Recipe is not null
        ? Recipe.Ingredients.Any(i => i.Food?.MilkPowder ?? false)
        : Food?.MilkPowder ?? false;
    public bool ContainsNuts => Recipe is not null
        ? Recipe.Ingredients.Any(i => i.Food?.Nuts ?? false)
        : Food?.Nuts ?? false;
}