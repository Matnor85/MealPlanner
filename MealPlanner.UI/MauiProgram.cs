using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPlanner.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "MealPlanner.db");

        // Factory i stället för AddDbContext: i en BlazorWebView lever ett scope
        // hela appens livstid, vilket ger en DbContext som växer okontrollerat.
        builder.Services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"));

        builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
        builder.Services.AddScoped<IFoodItemRepository, FoodItemRepository>();
        builder.Services.AddScoped<WeeklyPlannerService>();

        var app = builder.Build();

        //// Skapa databasen en gång vid start, inte vid varje sidladdning
        //using (var scope = app.Services.CreateScope())
        //{
        //    var factory = scope.ServiceProvider
        //        .GetRequiredService<IDbContextFactory<AppDbContext>>();
        //    using var context = factory.CreateDbContext();
        //    context.Database.EnsureCreated();
        //}
        using (var scope = app.Services.CreateScope())
        {
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = factory.CreateDbContext();

#if DEBUG
            // Bygger om databasen från noll vid varje start under utveckling.
            // TA BORT denna rad så fort du har data du vill behålla!
            context.Database.EnsureDeleted();
#endif

            context.Database.EnsureCreated();
        }

        return app;
    }
}