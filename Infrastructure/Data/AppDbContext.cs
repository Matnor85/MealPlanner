using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

// Denna klass representerar själva databasen och ärver från EF Cores DbContext
public class AppDbContext : DbContext
{
    // DbSet representerar tabellerna i din SQLite-databas.
    // Vi lägger bara till våra "Aggregate Roots" här. EF Core är smart nog 
    // att förstå att WeeklyPlan innehåller DailyPlan och Meal, och skapar 
    // de tabellerna automatiskt i bakgrunden!

    public DbSet<FoodItem> FoodItems { get; set; }
    public DbSet<WeeklyPlan> WeeklyPlans { get; set; }
    public DbSet<MonthlyPlan> MonthlyPlans { get; set; }

    // Om du vill ha en separat tabell för dina Fika-stunder!
    public DbSet<Fika> Fika { get; set; }

    // Konstruktor som tar emot inställningar (t.ex. sökvägen till SQLite-filen på telefonen)
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Här kan man detaljstyra hur databasen skapas (kallas Fluent API)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Exempel: Berätta för databasen att en FoodItem's Name är obligatorisk (inte null)
        modelBuilder.Entity<FoodItem>()
            .Property(f => f.Name)
            .IsRequired();

        // Här kan vi senare lägga in standard-matvaror (Havregryn, Ägg etc) 
        // så att databasen inte är helt tom första gången appen startas!
    }
}