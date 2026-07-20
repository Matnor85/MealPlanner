using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public class MonthlyPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Month { get; set; }
    public int Year { get; set; }

    public List<WeeklyPlan> Weeks { get; set; } = new();

    // Summary property to calculate total monthly calories
    public int TotalMonthlyCalories => Weeks.Sum(w => w.TotalWeeklyCalories);
}
