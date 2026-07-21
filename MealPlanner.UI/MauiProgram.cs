using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.External;
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

        // ----- Repositories -----

        builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
        builder.Services.AddScoped<IFoodItemRepository, FoodItemRepository>();
        builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
        builder.Services.AddScoped<IShoppingRepository, ShoppingRepository>();
        builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
        builder.Services.AddScoped<IPantryRepository, PantryRepository>();

        // ----- Tjänster -----

        builder.Services.AddScoped<WeeklyPlannerService>();
        builder.Services.AddScoped<ShoppingListService>();
        builder.Services.AddScoped<ProfileService>();
        builder.Services.AddScoped<PantryService>();
        builder.Services.AddScoped<RecipeSuggestionService>();

        // ---- Receptimport -----
        // Receptimport via bildtolkning. Nyckeln ligger tills vidare i appen -
        // flytta anropen till en backend innan appen delas med någon.
        builder.Services.AddSingleton<IRecipeImporter>(_ =>
            new GeminiRecipeImporter(
                new HttpClient { Timeout = TimeSpan.FromSeconds(60) },
                ApiKeys.Gemini));

        builder.Services.AddScoped<RecipeImportService>();

        // ----- Näringsuppslag -----

        // Lokal fil i stället för Livsmedelsverkets API, som returnerar
        // noll poster. Singleton eftersom filen läses in i minnet en gång.
        builder.Services.AddSingleton<INutritionLookup>(_ =>
            new LocalNutritionDatabase(
                () => FileSystem.OpenAppPackageFileAsync("livsmedel.json")));

        var app = builder.Build();

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