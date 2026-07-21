namespace Domain.Entities;

// Rått resultat från bildtolkningen, innan något matchats mot databasen.
public record ParsedIngredient(
    string Name,
    double Amount,
    string UnitText,
    double? EstimatedGrams);

public record ParsedStep(int Number, string Text, int? Minutes);

public record ParsedRecipe(
    string Name,
    int Portions,
    string? Note,
    List<ParsedIngredient> Ingredients,
    List<ParsedStep> Steps);