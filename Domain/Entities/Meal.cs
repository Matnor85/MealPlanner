using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public class Meal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // Varje måltid har en typ (standard är frukost om inget väljs)
    public MealType Type { get; set; } = MealType.Snack;

    // Måltider har en referens till en råvara (FoodItem) och en vikt i gram
    public FoodItem Food { get; set; } = new();
    public int WeightInGrams { get; set; }

    // Affärslogik för att beräkna kalorier baserat på vikt och kalorier per 100g
    public int CalculatedCalories => (int)Math.Round((double)Food.CaloriesPer100g * WeightInGrams / 100);
    
    // Genom att göra så här kan du enkelt fråga en Meal om den är vegansk,
    // men den hämtar egentligen svaret från den underliggande råvaran!
    public bool ContainsLactose => Food.Lactose;
    public bool ContainsGluten => Food.Gluten;
    public bool IsVegan => Food.Vegan;
    public bool ContainsMilkPowder => Food.MilkPowder;
    public bool ContainsNuts => Food.Nuts;
}
