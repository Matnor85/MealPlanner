namespace Domain.Entities;

// En träff i näringsdatabasen, innan användaren valt den.
public record NutritionSearchHit(string ExternalId, string Name);

// Hämtade näringsvärden per 100 g.
// OBS: null betyder "värde saknas", inte noll. Livsmedelsverket varnar
// uttryckligen för att tolka saknade värden som nollor - det ger fel
// vid kostberäkningar.
public record NutritionLookupResult(
    string ExternalId,
    string Name,
    FoodCategory Category,
    double? CaloriesPer100g,
    double? ProteinPer100g,
    double? FatPer100g,
    double? CarbsPer100g,
    double? FiberPer100g,
    double? SaltPer100g)
{
    public bool HasAnyValue =>
        CaloriesPer100g is not null || ProteinPer100g is not null ||
        FatPer100g is not null || CarbsPer100g is not null;

    // Vilka fält som saknades, för att kunna säga det till användaren
    public IEnumerable<string> MissingFields
    {
        get
        {
            if (CaloriesPer100g is null) yield return "kalorier";
            if (ProteinPer100g is null) yield return "protein";
            if (FatPer100g is null) yield return "fett";
            if (CarbsPer100g is null) yield return "kolhydrater";
            if (FiberPer100g is null) yield return "fiber";
            if (SaltPer100g is null) yield return "salt";
        }
    }
}