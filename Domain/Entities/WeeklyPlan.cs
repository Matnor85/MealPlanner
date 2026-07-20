using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public class WeeklyPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public List<DailyPlan> Days { get; set; } = new();

    public int TotalWeeklyCalories => Days.Sum(d => d.TotalCalories);
}
