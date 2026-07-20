namespace Domain.Entities;

public class RecipeStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }

    public int StepNumber { get; set; }
    public string Text { get; set; } = string.Empty;

    // Ungefärlig tidsåtgång, null om det inte är relevant
    public int? Minutes { get; set; }
}