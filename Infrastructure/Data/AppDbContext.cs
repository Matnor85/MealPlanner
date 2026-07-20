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

        // Startråvaror. Id:n måste vara fasta värden - inga Guid.NewGuid() här,
        // för då byter de identitet vid varje bygge.
        modelBuilder.Entity<FoodItem>().HasData(
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Havregryn", CaloriesPer100g = 370, Vegan = true, Gluten = true },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Ägg", CaloriesPer100g = 155 },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), Name = "Mellanmjölk", CaloriesPer100g = 45, Lactose = true },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), Name = "Kycklingfilé", CaloriesPer100g = 106 },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), Name = "Kokt potatis", CaloriesPer100g = 87, Vegan = true },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), Name = "Fullkornspasta", CaloriesPer100g = 350, Vegan = true, Gluten = true },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000007"), Name = "Banan", CaloriesPer100g = 89, Vegan = true },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000008"), Name = "Laxfilé", CaloriesPer100g = 208 },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000009"), Name = "Kanelbulle", CaloriesPer100g = 380, Gluten = true, Lactose = true },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-00000000000a"), Name = "Broccoli", CaloriesPer100g = 34, Vegan = true }
        );

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