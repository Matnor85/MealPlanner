using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan?> GetWeekAsync(int year, int weekNumber);
    Task SaveWeekAsync(WeeklyPlan weeklyPlan);
    Task<IEnumerable<WeeklyPlan>> GetAllWeeksAsync();
}
