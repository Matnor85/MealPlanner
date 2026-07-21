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
    public DbSet<ShoppingExtraItem> ShoppingExtras { get; set; }
    public DbSet<PantryItem> PantryItems { get; set; }

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

        // Utbytesgruppen slås upp ofta av receptförslagen
        modelBuilder.Entity<FoodItem>()
            .HasIndex(f => f.Substitutes);

        SeedFoods(modelBuilder);

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

        // ----- Skafferi -----

        // En rad per råvara. Mängder slås ihop i stället för att bli dubbletter.
        modelBuilder.Entity<PantryItem>()
            .HasIndex(p => p.FoodId)
            .IsUnique();

        modelBuilder.Entity<PantryItem>()
            .HasOne(p => p.Food)
            .WithMany()
            .HasForeignKey(p => p.FoodId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- Profil och invägningar -----

        modelBuilder.Entity<WeighIn>()
            .HasIndex(w => w.Date)
            .IsUnique();

        // ----- Inköpslista -----

        modelBuilder.Entity<ShoppingItemState>()
            .HasIndex(s => new { s.Year, s.WeekNumber, s.FoodId })
            .IsUnique();

        modelBuilder.Entity<ShoppingExtraItem>()
            .HasIndex(e => new { e.Year, e.WeekNumber });

        modelBuilder.Entity<ShoppingExtraItem>()
            .Property(e => e.Text)
            .IsRequired();
    }

    // Näringsvärden är ungefärliga och per 100 g.
    // GramsPerMilliliter är densiteten, GramsPerPiece styckvikten.
    private static void SeedFoods(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FoodItem>().HasData(
            new FoodItem
            {
                Id = F("01"),
                Name = "Havregryn",
                Category = FoodCategory.Grain,
                CaloriesPer100g = 370,
                ProteinPer100g = 13.5,
                FatPer100g = 7,
                CarbsPer100g = 59,
                FiberPer100g = 10,
                SaltPer100g = 0,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 0.4,
                Vegan = true,
                Gluten = true
            },

            new FoodItem
            {
                Id = F("02"),
                Name = "Ägg",
                Category = FoodCategory.Protein,
                CaloriesPer100g = 155,
                ProteinPer100g = 13,
                FatPer100g = 11,
                CarbsPer100g = 1.1,
                FiberPer100g = 0,
                SaltPer100g = 0.3,
                Unit = MeasureUnit.Piece,
                GramsPerPiece = 58
            },

            new FoodItem
            {
                Id = F("03"),
                Name = "Mellanmjölk",
                Category = FoodCategory.Dairy,
                CaloriesPer100g = 45,
                ProteinPer100g = 3.4,
                FatPer100g = 1.5,
                CarbsPer100g = 5,
                FiberPer100g = 0,
                SaltPer100g = 0.1,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 1.03,
                Lactose = true,
                Substitutes = SubstitutionGroup.MilkDrink
            },

            new FoodItem
            {
                Id = F("04"),
                Name = "Kycklingfilé",
                Category = FoodCategory.Protein,
                CaloriesPer100g = 106,
                ProteinPer100g = 23,
                FatPer100g = 1.5,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 0.15
            },

            new FoodItem
            {
                Id = F("05"),
                Name = "Kokt potatis",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 87,
                ProteinPer100g = 2,
                FatPer100g = 0.1,
                CarbsPer100g = 20,
                FiberPer100g = 1.8,
                SaltPer100g = 0.01,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("06"),
                Name = "Fullkornspasta",
                Category = FoodCategory.Grain,
                CaloriesPer100g = 350,
                ProteinPer100g = 13,
                FatPer100g = 2.5,
                CarbsPer100g = 62,
                FiberPer100g = 8,
                SaltPer100g = 0.02,
                Vegan = true,
                Gluten = true,
                Substitutes = SubstitutionGroup.PastaRice
            },

            new FoodItem
            {
                Id = F("07"),
                Name = "Banan",
                Category = FoodCategory.Fruit,
                CaloriesPer100g = 89,
                ProteinPer100g = 1.1,
                FatPer100g = 0.3,
                CarbsPer100g = 23,
                FiberPer100g = 2.6,
                SaltPer100g = 0,
                Unit = MeasureUnit.Piece,
                GramsPerPiece = 120,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("08"),
                Name = "Laxfilé",
                Category = FoodCategory.Protein,
                CaloriesPer100g = 208,
                ProteinPer100g = 20,
                FatPer100g = 13,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 0.1
            },

            new FoodItem
            {
                Id = F("09"),
                Name = "Kanelbulle",
                Category = FoodCategory.Treat,
                CaloriesPer100g = 380,
                ProteinPer100g = 7,
                FatPer100g = 14,
                CarbsPer100g = 55,
                FiberPer100g = 2.5,
                SaltPer100g = 0.9,
                Unit = MeasureUnit.Piece,
                GramsPerPiece = 70,
                Gluten = true,
                Lactose = true
            },

            new FoodItem
            {
                Id = F("0a"),
                Name = "Broccoli",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 34,
                ProteinPer100g = 2.8,
                FatPer100g = 0.4,
                CarbsPer100g = 7,
                FiberPer100g = 2.6,
                SaltPer100g = 0.03,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("0b"),
                Name = "Chokladkaka",
                Category = FoodCategory.Treat,
                CaloriesPer100g = 535,
                ProteinPer100g = 7.8,
                FatPer100g = 31,
                CarbsPer100g = 57,
                FiberPer100g = 3.4,
                SaltPer100g = 0.08,
                Lactose = true,
                MilkPowder = true
            },

            new FoodItem
            {
                Id = F("0c"),
                Name = "Kaffe, svart",
                Category = FoodCategory.Drink,
                CaloriesPer100g = 2,
                ProteinPer100g = 0.1,
                FatPer100g = 0,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 0,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 1,
                Vegan = true
            },

            // Råvaror till köttfärssåsen
            new FoodItem
            {
                Id = F("0d"),
                Name = "Spaghetti, torr",
                Category = FoodCategory.Grain,
                CaloriesPer100g = 355,
                ProteinPer100g = 12,
                FatPer100g = 1.5,
                CarbsPer100g = 71,
                FiberPer100g = 3,
                SaltPer100g = 0.01,
                Vegan = true,
                Gluten = true,
                Substitutes = SubstitutionGroup.PastaRice
            },

            new FoodItem
            {
                Id = F("0e"),
                Name = "Nötfärs 12%",
                Category = FoodCategory.Protein,
                CaloriesPer100g = 200,
                ProteinPer100g = 19,
                FatPer100g = 12,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 0.2,
                Substitutes = SubstitutionGroup.MincedBase
            },

            new FoodItem
            {
                Id = F("0f"),
                Name = "Krossade tomater",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 32,
                ProteinPer100g = 1.3,
                FatPer100g = 0.2,
                CarbsPer100g = 5,
                FiberPer100g = 1.4,
                SaltPer100g = 0.1,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 1.02,
                Vegan = true,
                Substitutes = SubstitutionGroup.TomatoBase
            },

            new FoodItem
            {
                Id = F("10"),
                Name = "Gul lök",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 40,
                ProteinPer100g = 1.1,
                FatPer100g = 0.1,
                CarbsPer100g = 9,
                FiberPer100g = 1.7,
                SaltPer100g = 0.01,
                Unit = MeasureUnit.Piece,
                GramsPerPiece = 110,
                Vegan = true,
                Substitutes = SubstitutionGroup.Onion
            },

            new FoodItem
            {
                Id = F("11"),
                Name = "Vitlöksklyfta",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 149,
                ProteinPer100g = 6.4,
                FatPer100g = 0.5,
                CarbsPer100g = 33,
                FiberPer100g = 2.1,
                SaltPer100g = 0.02,
                Unit = MeasureUnit.Piece,
                GramsPerPiece = 4,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("12"),
                Name = "Olivolja",
                Category = FoodCategory.Fat,
                CaloriesPer100g = 884,
                ProteinPer100g = 0,
                FatPer100g = 100,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 0,
                Unit = MeasureUnit.Tablespoon,
                GramsPerMilliliter = 0.91,
                Vegan = true,
                Substitutes = SubstitutionGroup.CookingFat
            },

            new FoodItem
            {
                Id = F("13"),
                Name = "Tomatpuré",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 82,
                ProteinPer100g = 4.3,
                FatPer100g = 0.5,
                CarbsPer100g = 16,
                FiberPer100g = 3.3,
                SaltPer100g = 0.1,
                Unit = MeasureUnit.Tablespoon,
                GramsPerMilliliter = 1.1,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("14"),
                Name = "Riven parmesan",
                Category = FoodCategory.Dairy,
                CaloriesPer100g = 431,
                ProteinPer100g = 38,
                FatPer100g = 29,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 1.6,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 0.4,
                Lactose = true,
                Substitutes = SubstitutionGroup.HardCheese
            },

            new FoodItem
            {
                Id = F("15"),
                Name = "Salt",
                Category = FoodCategory.Other,
                CaloriesPer100g = 0,
                ProteinPer100g = 0,
                FatPer100g = 0,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 100,
                Unit = MeasureUnit.Teaspoon,
                GramsPerMilliliter = 1.2,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("16"),
                Name = "Svartpeppar, malen",
                Category = FoodCategory.Other,
                CaloriesPer100g = 251,
                ProteinPer100g = 10,
                FatPer100g = 3.3,
                CarbsPer100g = 39,
                FiberPer100g = 25,
                SaltPer100g = 0.04,
                Unit = MeasureUnit.Pinch,
                GramsPerMilliliter = 0.5,
                Vegan = true
            },

            new FoodItem
            {
                Id = F("17"),
                Name = "Vetemjöl",
                Category = FoodCategory.Grain,
                CaloriesPer100g = 343,
                ProteinPer100g = 10,
                FatPer100g = 1.5,
                CarbsPer100g = 69,
                FiberPer100g = 3.5,
                SaltPer100g = 0,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 0.6,
                Vegan = true,
                Gluten = true,
                Substitutes = SubstitutionGroup.Flour
            },

            new FoodItem
            {
                Id = F("18"),
                Name = "Strösocker",
                Category = FoodCategory.Other,
                CaloriesPer100g = 400,
                ProteinPer100g = 0,
                FatPer100g = 0,
                CarbsPer100g = 100,
                FiberPer100g = 0,
                SaltPer100g = 0,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 0.85,
                Vegan = true,
                Substitutes = SubstitutionGroup.Sweetener
            },

            // Utbytesalternativ, så funktionen har något att arbeta med
            new FoodItem
            {
                Id = F("19"),
                Name = "Röda linser, torra",
                Category = FoodCategory.Protein,
                CaloriesPer100g = 350,
                ProteinPer100g = 24,
                FatPer100g = 1,
                CarbsPer100g = 60,
                FiberPer100g = 11,
                SaltPer100g = 0,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 0.85,
                Vegan = true,
                Substitutes = SubstitutionGroup.MincedBase
            },

            new FoodItem
            {
                Id = F("1a"),
                Name = "Svarta bönor, avrunna",
                Category = FoodCategory.Protein,
                CaloriesPer100g = 91,
                ProteinPer100g = 6,
                FatPer100g = 0.5,
                CarbsPer100g = 12,
                FiberPer100g = 7,
                SaltPer100g = 0.5,
                Vegan = true,
                Substitutes = SubstitutionGroup.MincedBase
            },

            new FoodItem
            {
                Id = F("1b"),
                Name = "Havredryck",
                Category = FoodCategory.Drink,
                CaloriesPer100g = 45,
                ProteinPer100g = 1,
                FatPer100g = 1.5,
                CarbsPer100g = 6.6,
                FiberPer100g = 0.8,
                SaltPer100g = 0.1,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 1.03,
                Vegan = true,
                Gluten = true,
                Substitutes = SubstitutionGroup.MilkDrink
            },

            new FoodItem
            {
                Id = F("1c"),
                Name = "Rapsolja",
                Category = FoodCategory.Fat,
                CaloriesPer100g = 900,
                ProteinPer100g = 0,
                FatPer100g = 100,
                CarbsPer100g = 0,
                FiberPer100g = 0,
                SaltPer100g = 0,
                Unit = MeasureUnit.Tablespoon,
                GramsPerMilliliter = 0.92,
                Vegan = true,
                Substitutes = SubstitutionGroup.CookingFat
            },

            new FoodItem
            {
                Id = F("1d"),
                Name = "Rödlök",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 42,
                ProteinPer100g = 1.2,
                FatPer100g = 0.1,
                CarbsPer100g = 9,
                FiberPer100g = 1.8,
                SaltPer100g = 0.01,
                Unit = MeasureUnit.Piece,
                GramsPerPiece = 100,
                Vegan = true,
                Substitutes = SubstitutionGroup.Onion
            },

            new FoodItem
            {
                Id = F("1e"),
                Name = "Passerade tomater",
                Category = FoodCategory.Vegetable,
                CaloriesPer100g = 29,
                ProteinPer100g = 1.2,
                FatPer100g = 0.2,
                CarbsPer100g = 4.5,
                FiberPer100g = 1.2,
                SaltPer100g = 0.1,
                Unit = MeasureUnit.Deciliter,
                GramsPerMilliliter = 1.03,
                Vegan = true,
                Substitutes = SubstitutionGroup.TomatoBase
            }
        );
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
            new RecipeIngredient { Id = Ing("04"), RecipeId = BologneseId, FoodId = F("10"), WeightInGrams = 110 },
            new RecipeIngredient { Id = Ing("05"), RecipeId = BologneseId, FoodId = F("11"), WeightInGrams = 8 },
            new RecipeIngredient { Id = Ing("06"), RecipeId = BologneseId, FoodId = F("12"), WeightInGrams = 18 },
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