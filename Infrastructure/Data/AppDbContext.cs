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

    // Fasta Id:n för seed-data. Måste vara konstanta - aldrig Guid.NewGuid().
    private static Guid F(string tail) => Guid.Parse($"11111111-0000-0000-0000-0000000000{tail}");
    private static readonly Guid BologneseId = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static Guid Ing(string tail) => Guid.Parse($"33333333-0000-0000-0000-0000000000{tail}");
    private static Guid Step(string tail) => Guid.Parse($"44444444-0000-0000-0000-0000000000{tail}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Råvaror -----

        modelBuilder.Entity<FoodItem>()
            .Property(f => f.Name)
            .IsRequired();

        modelBuilder.Entity<FoodItem>().HasData(
            new FoodItem { Id = F("01"), Name = "Havregryn", CaloriesPer100g = 370, Vegan = true, Gluten = true, Category = FoodCategory.Grain },
            new FoodItem { Id = F("02"), Name = "Ägg", CaloriesPer100g = 155, Category = FoodCategory.Protein },
            new FoodItem { Id = F("03"), Name = "Mellanmjölk", CaloriesPer100g = 45, Lactose = true, Category = FoodCategory.Dairy },
            new FoodItem { Id = F("04"), Name = "Kycklingfilé", CaloriesPer100g = 106, Category = FoodCategory.Protein },
            new FoodItem { Id = F("05"), Name = "Kokt potatis", CaloriesPer100g = 87, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = F("06"), Name = "Fullkornspasta", CaloriesPer100g = 350, Vegan = true, Gluten = true, Category = FoodCategory.Grain },
            new FoodItem { Id = F("07"), Name = "Banan", CaloriesPer100g = 89, Vegan = true, Category = FoodCategory.Fruit },
            new FoodItem { Id = F("08"), Name = "Laxfilé", CaloriesPer100g = 208, Category = FoodCategory.Protein },
            new FoodItem { Id = F("09"), Name = "Kanelbulle", CaloriesPer100g = 380, Gluten = true, Lactose = true, Category = FoodCategory.Treat },
            new FoodItem { Id = F("0a"), Name = "Broccoli", CaloriesPer100g = 34, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = F("0b"), Name = "Chokladkaka", CaloriesPer100g = 535, Lactose = true, MilkPowder = true, Category = FoodCategory.Treat },
            new FoodItem { Id = F("0c"), Name = "Kaffe, svart", CaloriesPer100g = 2, Vegan = true, Category = FoodCategory.Drink },

            // Råvaror till köttfärssåsen
            new FoodItem { Id = F("0d"), Name = "Spaghetti, torr", CaloriesPer100g = 355, Vegan = true, Gluten = true, Category = FoodCategory.Grain },
            new FoodItem { Id = F("0e"), Name = "Nötfärs 12%", CaloriesPer100g = 200, Category = FoodCategory.Protein },
            new FoodItem { Id = F("0f"), Name = "Krossade tomater", CaloriesPer100g = 32, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = F("10"), Name = "Gul lök", CaloriesPer100g = 40, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = F("11"), Name = "Vitlök", CaloriesPer100g = 149, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = F("12"), Name = "Olivolja", CaloriesPer100g = 884, Vegan = true, Category = FoodCategory.Fat },
            new FoodItem { Id = F("13"), Name = "Tomatpuré", CaloriesPer100g = 82, Vegan = true, Category = FoodCategory.Vegetable },
            new FoodItem { Id = F("14"), Name = "Riven parmesan", CaloriesPer100g = 431, Lactose = true, Category = FoodCategory.Dairy }
        );

        // ----- Veckor, dagar, måltider -----

        modelBuilder.Entity<WeeklyPlan>()
            .HasIndex(w => new { w.Year, w.WeekNumber })
            .IsUnique();

        modelBuilder.Entity<WeeklyPlan>()
            .HasMany(w => w.Days)
            .WithOne()
            .HasForeignKey(d => d.WeeklyPlanId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DailyPlan>()
            .HasMany(d => d.Meals)
            .WithOne()
            .HasForeignKey(m => m.DailyPlanId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Meal>()
            .HasOne(m => m.Food)
            .WithMany()
            .HasForeignKey(m => m.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Meal>()
            .HasOne(m => m.Recipe)
            .WithMany()
            .HasForeignKey(m => m.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Meal>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Meal_FoodXorRecipe",
                "(FoodId IS NULL) <> (RecipeId IS NULL)"));

        // ----- Recept -----

        modelBuilder.Entity<Recipe>()
            .Property(r => r.Name)
            .IsRequired();

        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecipeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(i => i.Food)
            .WithMany()
            .HasForeignKey(i => i.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        SeedBolognese(modelBuilder);

        // ----- Profil och invägningar -----

        modelBuilder.Entity<WeighIn>()
            .HasIndex(w => w.Date)
            .IsUnique();

        // ----- Inköpslista -----

        modelBuilder.Entity<ShoppingItemState>()
            .HasIndex(s => new { s.Year, s.WeekNumber, s.FoodId })
            .IsUnique();
    }

    private static void SeedBolognese(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>().HasData(new Recipe
        {
            Id = BologneseId,
            Name = "Spaghetti med köttfärssås",
            Portions = 4,
            Instructions = "Såsen blir bättre ju längre den får puttra. Har du tid, låt den gå en timme."
        });

        // Mängderna gäller HELA receptet, alltså alla fyra portionerna
        modelBuilder.Entity<RecipeIngredient>().HasData(
            new RecipeIngredient { Id = Ing("01"), RecipeId = BologneseId, FoodId = F("0d"), WeightInGrams = 400 },
            new RecipeIngredient { Id = Ing("02"), RecipeId = BologneseId, FoodId = F("0e"), WeightInGrams = 500 },
            new RecipeIngredient { Id = Ing("03"), RecipeId = BologneseId, FoodId = F("0f"), WeightInGrams = 400 },
            new RecipeIngredient { Id = Ing("04"), RecipeId = BologneseId, FoodId = F("10"), WeightInGrams = 100 },
            new RecipeIngredient { Id = Ing("05"), RecipeId = BologneseId, FoodId = F("11"), WeightInGrams = 10 },
            new RecipeIngredient { Id = Ing("06"), RecipeId = BologneseId, FoodId = F("12"), WeightInGrams = 20 },
            new RecipeIngredient { Id = Ing("07"), RecipeId = BologneseId, FoodId = F("13"), WeightInGrams = 40 },
            new RecipeIngredient { Id = Ing("08"), RecipeId = BologneseId, FoodId = F("14"), WeightInGrams = 40 }
        );

        modelBuilder.Entity<RecipeStep>().HasData(
            new RecipeStep
            {
                Id = Step("01"),
                RecipeId = BologneseId,
                StepNumber = 1,
                Minutes = 5,
                Text = "Skala och finhacka löken. Pressa eller finhacka vitlöken. Mät upp resten så du har allt framme."
            },

            new RecipeStep
            {
                Id = Step("02"),
                RecipeId = BologneseId,
                StepNumber = 2,
                Minutes = 5,
                Text = "Hetta upp olivoljan i en rymlig gryta på medelvärme. Fräs löken mjuk och blank utan att den tar färg, cirka 4 minuter. Tillsätt vitlöken den sista minuten."
            },

            new RecipeStep
            {
                Id = Step("03"),
                RecipeId = BologneseId,
                StepNumber = 3,
                Minutes = 8,
                Text = "Höj värmen och lägg i nötfärsen. Bryt sönder den med en slev och låt den bryna ordentligt tills vätskan kokat bort och färsen börjar få färg. Stressa inte - det är här smaken byggs."
            },

            new RecipeStep
            {
                Id = Step("04"),
                RecipeId = BologneseId,
                StepNumber = 4,
                Minutes = 2,
                Text = "Rör ner tomatpurén och låt den fräsa med i en minut. Det tar bort den råa syrligheten."
            },

            new RecipeStep
            {
                Id = Step("05"),
                RecipeId = BologneseId,
                StepNumber = 5,
                Minutes = 30,
                Text = "Häll i de krossade tomaterna. Salta och peppra. Sänk värmen och låt såsen puttra utan lock i minst 30 minuter, gärna längre. Rör om då och då."
            },

            new RecipeStep
            {
                Id = Step("06"),
                RecipeId = BologneseId,
                StepNumber = 6,
                Minutes = 10,
                Text = "Koka upp rikligt med vatten i en stor kastrull. Salta ordentligt - vattnet ska smaka hav. Koka spaghettin enligt tiden på paketet, men börja smaka av en minut innan."
            },

            new RecipeStep
            {
                Id = Step("07"),
                RecipeId = BologneseId,
                StepNumber = 7,
                Minutes = 2,
                Text = "Spara en skvätt pastavatten innan du häller av. Blanda pastan med såsen i grytan och späd med pastavattnet tills såsen sitter på pastan i stället för att rinna av."
            },

            new RecipeStep
            {
                Id = Step("08"),
                RecipeId = BologneseId,
                StepNumber = 8,
                Minutes = 1,
                Text = "Smaka av med salt och peppar. Servera direkt med riven parmesan över."
            }
        );
    }
}