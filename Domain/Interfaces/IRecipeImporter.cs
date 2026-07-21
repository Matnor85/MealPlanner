using Domain.Entities;

namespace Domain.Interfaces;

// Port mot en bildtolkningstjänst. Implementationen ligger i Infrastructure,
// så att byte av leverantör - eller flytt till en egen backend - bara rör en fil.
public interface IRecipeImporter
{
    Task<ParsedRecipe> FromImageAsync(
        byte[] imageBytes, string mimeType, CancellationToken ct = default);

    Task<ParsedRecipe> FromTextAsync(string text, CancellationToken ct = default);

    bool IsConfigured { get; }
}