using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public record TrendPoint(DateOnly Date, double Raw, double Average);

public class ProfileService
{
    private readonly IProfileRepository _repository;

    public ProfileService(IProfileRepository repository) => _repository = repository;

    public Task<UserProfile?> GetAsync() => _repository.GetAsync();
    public Task SaveAsync(UserProfile profile) => _repository.SaveAsync(profile);
    public Task DeleteWeighInAsync(Guid id) => _repository.DeleteWeighInAsync(id);

    public Task AddWeighInAsync(DateOnly date, double weightKg, string? note = null)
    {
        if (weightKg <= 0 || weightKg > 500)
            throw new ArgumentException("Ange en rimlig vikt i kilo.", nameof(weightKg));

        return _repository.AddOrUpdateWeighInAsync(date, weightKg, note);
    }

    public async Task<List<WeighIn>> GetWeighInsAsync() =>
        (await _repository.GetWeighInsAsync()).ToList();

    // Dagsvikt hoppar ett par kilo av rena vätskeskäl. Ett glidande medelvärde
    // över sju dagar visar den faktiska riktningen i stället för bruset.
    public static List<TrendPoint> BuildTrend(List<WeighIn> weighIns, int window = 7)
    {
        var result = new List<TrendPoint>();

        for (int i = 0; i < weighIns.Count; i++)
        {
            int from = Math.Max(0, i - window + 1);
            double avg = weighIns.Skip(from).Take(i - from + 1).Average(w => w.WeightKg);
            result.Add(new TrendPoint(weighIns[i].Date, weighIns[i].WeightKg, Math.Round(avg, 1)));
        }

        return result;
    }

    // Mifflin-St Jeor. Grundomsättning - vad kroppen gör av med i total vila.
    public static int CalculateBmr(UserProfile profile, double weightKg)
    {
        if (profile.HeightInCm <= 0 || weightKg <= 0 || profile.Age <= 0) return 0;

        double bmr = 10 * weightKg + 6.25 * profile.HeightInCm - 5 * profile.Age;

        bmr += profile.Sex switch
        {
            Sex.Male => 5,
            Sex.Female => -161,
            _ => -78   // mittemellan när kön inte angetts
        };

        return (int)Math.Round(bmr);
    }

    // Totalt dagsbehov med aktivitetsnivån inräknad.
    public static int CalculateTdee(UserProfile profile, double weightKg)
    {
        int bmr = CalculateBmr(profile, weightKg);
        if (bmr == 0) return 0;

        double factor = profile.Activity switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.Light => 1.375,
            ActivityLevel.Moderate => 1.55,
            ActivityLevel.Active => 1.725,
            _ => 1.9
        };

        return (int)Math.Round(bmr * factor);
    }

    // Ett måttligt underskott om målvikten ligger under nuvarande, annars underhåll.
    public static int SuggestGoal(UserProfile profile, double weightKg)
    {
        int tdee = CalculateTdee(profile, weightKg);
        if (tdee == 0) return 0;

        bool wantsLoss = profile.GoalWeightKg is double g && g < weightKg - 0.5;
        int suggested = wantsLoss ? tdee - 400 : tdee;

        // Golv vid grundomsättningen. Att äta under den under längre tid
        // är inte en snabbare väg till målet, bara en sämre.
        int floor = CalculateBmr(profile, weightKg);
        return Math.Max(suggested, floor);
    }

    public static bool IsGoalTooLow(UserProfile profile, double weightKg)
    {
        if (profile.DailyCalorieGoal is not int goal) return false;
        int bmr = CalculateBmr(profile, weightKg);
        return bmr > 0 && goal < bmr;
    }
}