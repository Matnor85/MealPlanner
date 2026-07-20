using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<FoodItem> FoodItems { get; set; }
    public DbSet<WeeklyPlan> WeeklyPlans { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<WeighIn> WeighIns { get; set; }
    public DbSet<ShoppingItemState> ShoppingItems { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Råvaror -----

        modelBuilder.Entity<FoodItem>()
            .Property(f => f.Name)
            .IsRequired();

        // Startråvaror. Id:n måste vara fasta värden - inga Guid.NewGuid() här,
        // för då byter de identitet vid varje bygge.
        modelBuilder.Entity<FoodItem>().HasData(
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Havregryn", CaloriesPer100g = 370, Vegan = true, Gluten = true, Category = FoodCategory.Grain },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Ägg", CaloriesPer100g = 155, Category = FoodCategory.Protein },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), Name = "Mellanmjölk", CaloriesPer100g = 45, Lactose = true, Category = FoodCategory.Dairy },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), Name = "Kycklingfilé", CaloriesPer100g = 106, Category = FoodCategory.Protein },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), Name = "Kokt potatis", CaloriesPer100g = 87, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), Name = "Fullkornspasta", CaloriesPer100g = 350, Vegan = true, Gluten = true, Category = FoodCategory.Grain },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000007"), Name = "Banan", CaloriesPer100g = 89, Vegan = true, Category = FoodCategory.Fruit },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000008"), Name = "Laxfilé", CaloriesPer100g = 208, Category = FoodCategory.Protein },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-000000000009"), Name = "Kanelbulle", CaloriesPer100g = 380, Gluten = true, Lactose = true, Category = FoodCategory.Treat },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-00000000000a"), Name = "Broccoli", CaloriesPer100g = 34, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-00000000000b"), Name = "Chokladkaka", CaloriesPer100g = 535, Lactose = true, MilkPowder = true, Category = FoodCategory.Treat },
            new FoodItem { Id = Guid.Parse("11111111-0000-0000-0000-00000000000c"), Name = "Kaffe, svart", CaloriesPer100g = 2, Vegan = true, Category = FoodCategory.Drink }
        );

        // ----- Veckor, dagar, måltider -----

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

        // Samma sak för receptet - måltiden pekar bara på det.
        modelBuilder.Entity<Meal>()
            .HasOne(m => m.Recipe)
            .WithMany()
            .HasForeignKey(m => m.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Databasen bevakar samma regel som Meal.IsValid:
        // exakt en av FoodId och RecipeId måste vara satt.
        modelBuilder.Entity<Meal>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Meal_FoodXorRecipe",
                "(FoodId IS NULL) <> (RecipeId IS NULL)"));

        // ----- Recept -----

        modelBuilder.Entity<Recipe>()
            .Property(r => r.Name)
            .IsRequired();

        // Ett recept äger sina ingredienser
        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Råvaran i en ingrediens raderas aldrig automatiskt
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(i => i.Food)
            .WithMany()
            .HasForeignKey(i => i.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        // ----- Profil och invägningar -----

        // Bara en invägning per datum
        modelBuilder.Entity<WeighIn>()
            .HasIndex(w => w.Date)
            .IsUnique();

        // ----- Inköpslista -----

        // En avbockning per råvara och vecka
        modelBuilder.Entity<ShoppingItemState>()
            .HasIndex(s => new { s.Year, s.WeekNumber, s.FoodId })
            .IsUnique();
    }
}