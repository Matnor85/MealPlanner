namespace Domain.Entities;

// Ett näringsinnehåll. Kan summeras och skalas, vilket gör att måltider,
// dagar, veckor och recept alla kan räknas fram med samma logik.
public readonly record struct Nutrition(
    int Calories,
    double Protein,
    double Fat,
    double Carbs,
    double Fiber,
    double Salt)
{
    public static Nutrition Zero => default;

    public static Nutrition operator +(Nutrition a, Nutrition b) => new(
        a.Calories + b.Calories,
        a.Protein + b.Protein,
        a.Fat + b.Fat,
        a.Carbs + b.Carbs,
        a.Fiber + b.Fiber,
        a.Salt + b.Salt);

    public static Nutrition operator *(Nutrition n, double factor) => new(
        (int)Math.Round(n.Calories * factor),
        n.Protein * factor,
        n.Fat * factor,
        n.Carbs * factor,
        n.Fiber * factor,
        n.Salt * factor);

    public Nutrition Rounded() => new(
        Calories,
        Math.Round(Protein, 1),
        Math.Round(Fat, 1),
        Math.Round(Carbs, 1),
        Math.Round(Fiber, 1),
        Math.Round(Salt, 2));

    public static Nutrition Sum(IEnumerable<Nutrition> items)
    {
        var total = Zero;
        foreach (var item in items) total += item;
        return total;
    }
}