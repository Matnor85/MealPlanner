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

    // Används bara när måltiden är en enskild råvara
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
        : $"{WeightInGrams} g";

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

    // ----- Allergier och kost -----

    public bool ContainsLactose => AnyIngredient(f => f.Lactose);
    public bool ContainsGluten => AnyIngredient(f => f.Gluten);
    public bool ContainsMilkPowder => AnyIngredient(f => f.MilkPowder);
    public bool ContainsNuts => AnyIngredient(f => f.Nuts);

    public bool IsVegan
    {
        get
        {
            if (Recipe is not null)
            {
                GuardRecipeLoaded();

                // All() på en tom lista ger true - ett tomt recept är inte veganskt
                if (Recipe.Ingredients.Count == 0) return false;

                return Recipe.Ingredients.All(i => i.Food!.Vegan);
            }

            GuardFoodLoaded();
            return Food?.Vegan ?? false;
        }
    }

    // Svarar aldrig "nej" på en allergifråga när underlaget saknas.
    // Ett undantag under utveckling är bättre än ett tyst felaktigt svar.
    private bool AnyIngredient(Func<FoodItem, bool> predicate)
    {
        if (Recipe is not null)
        {
            GuardRecipeLoaded();
            return Recipe.Ingredients.Any(i => predicate(i.Food!));
        }

        GuardFoodLoaded();
        return Food is not null && predicate(Food);
    }

    private void GuardRecipeLoaded()
    {
        if (Recipe!.Ingredients.Any(i => i.Food is null))
            throw new InvalidOperationException(
                $"Receptet '{Recipe.Name}' har ingredienser utan laddad råvara. " +
                "Kontrollera att queryn har Include på RecipeIngredient.Food.");
    }

    private void GuardFoodLoaded()
    {
        if (FoodId.HasValue && Food is null)
            throw new InvalidOperationException(
                "Måltidens råvara är inte laddad. Kontrollera att queryn har Include på Meal.Food.");
    }
}