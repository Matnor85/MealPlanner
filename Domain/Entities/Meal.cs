namespace Domain.Entities;

public class Meal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Främmande nyckel till dagen måltiden tillhör
    public Guid DailyPlanId { get; set; }

    // Standard är mellanmål om inget väljs
    public MealType Type { get; set; } = MealType.Snack;

    // En måltid är ANTINGEN en enskild råvara ELLER ett recept - aldrig båda.
    // Båda är nullable eftersom måltiden kan skapas innan valet är gjort.

    public Guid? FoodId { get; set; }
    public FoodItem? Food { get; set; }

    // Används bara när måltiden är en enskild råvara. Alltid gram.
    public int WeightInGrams { get; set; }

    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Används bara när måltiden är ett recept
    public double Portions { get; set; } = 1;

    // ----- Härledda egenskaper -----

    public bool IsRecipeMeal => RecipeId.HasValue;

    // Exakt en av FoodId och RecipeId måste vara satt
    public bool IsValid => RecipeId.HasValue ^ FoodId.HasValue;

    public string DisplayName => Recipe?.Name ?? Food?.Name ?? "Inget valt";

    public string DisplayAmount => IsRecipeMeal
        ? $"{Portions:0.##} port."
        : Food?.FormatAmount(WeightInGrams) ?? $"{WeightInGrams} g";

    public Nutrition Nutrition
    {
        get
        {
            GuardLoaded();

            if (Recipe is not null)
                return (Recipe.NutritionPerPortion * Portions).Rounded();

            if (Food is not null)
                return Food.ForGrams(WeightInGrams).Rounded();

            return Nutrition.Zero;
        }
    }

    public int CalculatedCalories => Nutrition.Calories;

    // ----- Allergier och kost -----

    public bool ContainsLactose => AnyIngredient(f => f.Lactose);
    public bool ContainsGluten => AnyIngredient(f => f.Gluten);
    public bool ContainsMilkPowder => AnyIngredient(f => f.MilkPowder);
    public bool ContainsNuts => AnyIngredient(f => f.Nuts);

    public bool IsVegan
    {
        get
        {
            GuardLoaded();

            if (Recipe is not null)
            {
                // All() på en tom lista ger true - ett tomt recept är inte veganskt
                if (Recipe.Ingredients.Count == 0) return false;

                return Recipe.Ingredients.All(i => i.Food!.Vegan);
            }

            return Food?.Vegan ?? false;
        }
    }

    private bool AnyIngredient(Func<FoodItem, bool> predicate)
    {
        GuardLoaded();

        if (Recipe is not null)
            return Recipe.Ingredients.Any(i => predicate(i.Food!));

        return Food is not null && predicate(Food);
    }

    // En enda vakt för allt. Måltiden svarar hellre med undantag än med fel siffra
    // eller ett tyst "nej" på en allergifråga.
    private void GuardLoaded()
    {
        if (RecipeId.HasValue && Recipe is null)
            throw new InvalidOperationException(
                "Måltidens recept är inte laddat. Kontrollera att queryn har Include på Meal.Recipe.");

        if (Recipe is not null && Recipe.Ingredients.Any(i => i.Food is null))
            throw new InvalidOperationException(
                $"Receptet '{Recipe.Name}' har ingredienser utan laddad råvara. " +
                "Kontrollera att queryn har Include på RecipeIngredient.Food.");

        if (FoodId.HasValue && Food is null)
            throw new InvalidOperationException(
                "Måltidens råvara är inte laddad. Kontrollera att queryn har Include på Meal.Food.");
    }
}