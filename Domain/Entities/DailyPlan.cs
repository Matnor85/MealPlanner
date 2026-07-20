using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public class DailyPlan
{
    public DayOfWeek Day { get; set; }
    public List<Meal> Meals { get; set; } = new();
    
    public int TotalCalories => Meals.Sum(m => m.CalculatedCalories);
}
