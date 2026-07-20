using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class WeeklyPlannerService
{
    private readonly IWeeklyPlanRepository _repository;

    // Dependency Injection: Vi skickar in databas-hanteraren, 
    // vilket gör denna tjänst otroligt enkel att enhetstesta!
    public WeeklyPlannerService(IWeeklyPlanRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<WeeklyPlan> GetOrCreateWeekAsync(int year, int weekNumber)
    {
        var existingWeek = await _repository.GetWeekAsync(year, weekNumber);

        if (existingWeek != null)
            return existingWeek;

        // Skapa en tom vecka om den inte finns
        var newWeek = new WeeklyPlan { Year = year, WeekNumber = weekNumber };
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            newWeek.Days.Add(new DailyPlan { Day = day });
        }

        return newWeek;
    }

    // Use Case: Kopiera en vecka till nästa
    public async Task<WeeklyPlan> CopyWeekToNextAsync(int fromYear, int fromWeekNumber)
    {
        var sourceWeek = await _repository.GetWeekAsync(fromYear, fromWeekNumber);
        if (sourceWeek == null)
        {
            throw new InvalidOperationException("Källveckan finns inte.");
        }

        // Räkna ut nästa vecka (förenklad logik för att hantera årsskiften)
        int targetWeek = sourceWeek.WeekNumber == 52 ? 1 : sourceWeek.WeekNumber + 1;
        int targetYear = sourceWeek.WeekNumber == 52 ? sourceWeek.Year + 1 : sourceWeek.Year;

        // Skapa en djupkopia av måltiderna
        var newWeek = new WeeklyPlan { Year = targetYear, WeekNumber = targetWeek };
        foreach (var sourceDay in sourceWeek.Days)
        {
            var newDay = new DailyPlan { Day = sourceDay.Day };
            foreach (var sourceMeal in sourceDay.Meals)
            {
                newDay.Meals.Add(new Meal
                {
                    Food = sourceMeal.Food, // Samma råvara
                    WeightInGrams = sourceMeal.WeightInGrams
                });
            }
            newWeek.Days.Add(newDay);
        }

        // Spara den nya kopierade veckan
        await _repository.SaveWeekAsync(newWeek);

        return newWeek;
    }
}