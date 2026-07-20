using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public record FoodItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public int CaloriesPer100g { get; init; }

    // Allergens and dietary restrictions
    public bool Lactose { get; init; }
    public bool Gluten { get; init; }
    public bool Vegan { get; init; }
    public bool MilkPowder { get; init; }
    public bool Nuts { get; init; }
}
