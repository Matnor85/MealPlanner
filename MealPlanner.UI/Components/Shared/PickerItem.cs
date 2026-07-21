namespace MealPlanner.UI.Components.Shared;

// En rad i sökväljaren. Sub är den grå undertexten, t.ex. "89 kcal/100 g".
public record PickerItem(Guid Id, string Label, string? Sub = null);