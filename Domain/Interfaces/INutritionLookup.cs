using Domain.Entities;

namespace Domain.Interfaces;

// Port mot en extern näringsdatabas. Implementationen ligger i Infrastructure,
// så att byte av leverantör bara rör en fil.
public interface INutritionLookup
{
    Task<IReadOnlyList<NutritionSearchHit>> SearchAsync(string query, CancellationToken ct = default);

    Task<NutritionLookupResult?> GetAsync(string externalId, CancellationToken ct = default);

    // Källhänvisning som måste visas i appen enligt licensvillkoren
    string Attribution { get; }
}