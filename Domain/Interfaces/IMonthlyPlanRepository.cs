using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IMonthlyPlanRepository
{
    Task<MonthlyPlan?> GetMonthAsync(int year, int month);
    Task SaveMonthAsync(MonthlyPlan monthlyPlan);
    Task<IEnumerable<MonthlyPlan>> GetAllMonthsAsync();
}
