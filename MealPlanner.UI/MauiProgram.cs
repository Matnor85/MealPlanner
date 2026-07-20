using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Application.Services;

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

        // 1. Skapa en säker sökväg till databasfilen för iOS/Android/Windows
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "MealPlanner.db");

        // 2. Berätta för Entity Framework att använda SQLite med denna fil
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"));

        // 3. Dependency Injection! 
        // AddScoped betyder att den lever så länge användaren är inne på en specifik sida/vy.
        builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();

        // Här kan du senare lägga till IFoodItemRepository etc...
        // builder.Services.AddScoped<IFoodItemRepository, FoodItemRepository>();

        // Registrera din service från Application-lagret
        builder.Services.AddScoped<WeeklyPlannerService>();

        return builder.Build();
    }
}
