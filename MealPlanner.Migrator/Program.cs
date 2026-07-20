using Infrastructure.Data;
using MealPlanner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// Här skapar vi en tillfällig kontext så att EF Core kan bygga databasen
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlite("Data Source=../MealPlanner.db"); // Spara i rotmappen under utveckling

using (var context = new AppDbContext(optionsBuilder.Options))
{
    // Detta kommando skapar tabellerna
    context.Database.EnsureCreated();
}