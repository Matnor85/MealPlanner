using Domain.Entities;

namespace Domain.Interfaces;

// Port mot en näringsdatabas. Implementationen ligger i Infrastructure,
// så att byte av källa bara rör en fil.
public interface INutritionLookup
{
    Task<IReadOnlyList<NutritionSearchHit>> SearchAsync(string query, CancellationToken ct = default);

    // Hela databasen. Används av importen, som poängsätter alla kandidater
    // själv i stället för att lita på sökningens ordning.
    Task<IReadOnlyList<NutritionSearchHit>> GetAllAsync(CancellationToken ct = default);

    Task<NutritionLookupResult?> GetAsync(string externalId, CancellationToken ct = default);

    // Antal poster i källan. Används för felsökning - noll betyder att
    // datafilen inte gick att läsa.
    Task<int> CountAsync(CancellationToken ct = default);

    // Källhänvisning som måste visas i appen enligt licensvillkoren
    string Attribution { get; }
}