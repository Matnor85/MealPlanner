using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<FoodItem> FoodItems { get; set; }
    public DbSet<WeeklyPlan> WeeklyPlans { get; set; }
    public DbSet<MonthlyPlan> MonthlyPlans { get; set; }
    public DbSet<Fika> Fikas { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FoodItem>()
            .Property(f => f.Name)
            .IsRequired();

        // Ett år + veckonummer får bara finnas en gång
        modelBuilder.Entity<WeeklyPlan>()
            .HasIndex(w => new { w.Year, w.WeekNumber })
            .IsUnique();

        // En dag måste tillhöra en vecka. Raderas veckan följer dagarna med.
        modelBuilder.Entity<WeeklyPlan>()
            .HasMany(w => w.Days)
            .WithOne()
            .HasForeignKey(d => d.WeeklyPlanId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // En måltid måste tillhöra en dag. Raderas dagen följer måltiderna med.
        modelBuilder.Entity<DailyPlan>()
            .HasMany(d => d.Meals)
            .WithOne()
            .HasForeignKey(m => m.DailyPlanId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Råvaran däremot ska ALDRIG raderas bara för att en måltid försvinner.
        modelBuilder.Entity<Meal>()
            .HasOne(m => m.Food)
            .WithMany()
            .HasForeignKey(m => m.FoodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}