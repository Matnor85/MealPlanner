using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Denna klass implementerar interfacet från vår innersta Domain-kärna
public class WeeklyPlanRepository : IWeeklyPlanRepository
{
    private readonly AppDbContext _context;

    // Vi injicerar vår databaskontext via konstruktorn
    public WeeklyPlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WeeklyPlan?> GetWeekAsync(int year, int weekNumber)
    {
        // VIKTIGT: EF Core laddar inte relaterad data automatiskt av prestandaskäl (Lazy Loading).
        // Eftersom WeeklyPlan innehåller Days, som innehåller Meals, som innehåller Food,
        // måste vi berätta för databasen att vi vill hämta ("Include") hela trädet på en gång!
        return await _context.WeeklyPlans
            .Include(w => w.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Food)
            // OBS: Kolla så att du faktiskt har 'public int Month { get; set; }' i din WeeklyPlan.cs-entitet,
            // annars kan du ta bort 'w.Month == month' här nedanför.
            .FirstOrDefaultAsync(w => w.Year == year && w.WeekNumber == weekNumber);
    }

    public async Task SaveWeekAsync(WeeklyPlan weeklyPlan)
    {
        // Kolla om veckan redan existerar i databasen
        var existingWeek = await _context.WeeklyPlans.AsNoTracking().FirstOrDefaultAsync(w => w.Id == weeklyPlan.Id);

        if (existingWeek == null)
        {
            // Om den inte finns, lägg till den som ny
            await _context.WeeklyPlans.AddAsync(weeklyPlan);
        }
        else
        {
            // Om den redan finns, uppdatera den
            _context.WeeklyPlans.Update(weeklyPlan);
        }

        // Utför själva sparandet i SQLite-filen
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WeeklyPlan>> GetAllWeeksAsync()
    {
        return await _context.WeeklyPlans
            .Include(w => w.Days)
            .ToListAsync();
    }
}