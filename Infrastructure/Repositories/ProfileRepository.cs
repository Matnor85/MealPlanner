using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProfileRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    // Det finns bara en profil i appen - vi hämtar alltid den första.
    public async Task<UserProfile?> GetAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.UserProfiles.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task SaveAsync(UserProfile profile)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var existing = await context.UserProfiles.FirstOrDefaultAsync();

        if (existing is null)
        {
            context.UserProfiles.Add(profile);
        }
        else
        {
            existing.Name = profile.Name;
            existing.Sex = profile.Sex;
            existing.BirthDate = profile.BirthDate;
            existing.HeightInCm = profile.HeightInCm;
            existing.StartWeightKg = profile.StartWeightKg;
            existing.GoalWeightKg = profile.GoalWeightKg;
            existing.Activity = profile.Activity;
            existing.DailyCalorieGoal = profile.DailyCalorieGoal;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WeighIn>> GetWeighInsAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.WeighIns
            .AsNoTracking()
            .OrderBy(w => w.Date)
            .ToListAsync();
    }

    // Unikt index på Date - en invägning per dag, senaste vinner.
    public async Task AddOrUpdateWeighInAsync(DateOnly date, double weightKg, string? note)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var existing = await context.WeighIns.FirstOrDefaultAsync(w => w.Date == date);

        if (existing is null)
        {
            context.WeighIns.Add(new WeighIn { Date = date, WeightKg = weightKg, Note = note });
        }
        else
        {
            existing.WeightKg = weightKg;
            existing.Note = note;
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteWeighInAsync(Guid id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var w = await context.WeighIns.FirstOrDefaultAsync(x => x.Id == id);
        if (w is not null)
        {
            context.WeighIns.Remove(w);
            await context.SaveChangesAsync();
        }
    }
}