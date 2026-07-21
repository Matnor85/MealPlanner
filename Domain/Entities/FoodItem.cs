namespace Domain.Entities;

public record FoodItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public FoodCategory Category { get; init; } = FoodCategory.Other;

    // Vilka råvaror som kan ersätta den här. None = inga utbyten.
    public SubstitutionGroup Substitutes { get; init; } = SubstitutionGroup.None;

    // ----- Näring per 100 g -----

    public int CaloriesPer100g { get; init; }
    public double ProteinPer100g { get; init; }
    public double FatPer100g { get; init; }
    public double CarbsPer100g { get; init; }
    public double FiberPer100g { get; init; }
    public double SaltPer100g { get; init; }

    // ----- Mängd -----

    public MeasureUnit Unit { get; init; } = MeasureUnit.Gram;

    // Densitet i g/ml. Vatten = 1, mjöl ≈ 0,6, strösocker ≈ 0,85.
    // Ett enda värde räcker för ml, dl, msk, tsk och krm.
    public double GramsPerMilliliter { get; init; } = 1;

    // Vad ett stycke väger. Ett ägg ≈ 58 g, en banan ≈ 120 g.
    public double GramsPerPiece { get; init; } = 100;

    // ----- Allergener -----

    public bool Lactose { get; init; }
    public bool Gluten { get; init; }
    public bool Vegan { get; init; }
    public bool MilkPowder { get; init; }
    public bool Nuts { get; init; }

    // ----- Näringsberäkning -----

    public Nutrition Per100g => new(
        CaloriesPer100g, ProteinPer100g, FatPer100g,
        CarbsPer100g, FiberPer100g, SaltPer100g);

    public Nutrition ForGrams(double grams) => Per100g * (grams / 100.0);

    // ----- Omräkning -----

    private double SafeDensity => GramsPerMilliliter > 0 ? GramsPerMilliliter : 1;
    private double SafePieceWeight => GramsPerPiece > 0 ? GramsPerPiece : 1;

    // Vad EN enhet av det angivna måttet väger i gram
    public double GramsPer(MeasureUnit unit) => unit switch
    {
        MeasureUnit.Gram => 1,
        MeasureUnit.Piece => SafePieceWeight,
        _ => unit.MilliliterFactor() * SafeDensity
    };

    public double ToGrams(double amount, MeasureUnit unit) => amount * GramsPer(unit);
    public double FromGrams(double grams, MeasureUnit unit) => grams / GramsPer(unit);

    // Samma sak fast med råvarans egen enhet
    public double ToGrams(double amount) => ToGrams(amount, Unit);
    public double FromGrams(double grams) => FromGrams(grams, Unit);

    public string UnitLabel => Unit.Label();

    // Vilka mått som är rimliga att välja för just den här råvaran.
    // Gram finns alltid med som reserv.
    public IEnumerable<MeasureUnit> AvailableUnits
    {
        get
        {
            yield return MeasureUnit.Gram;

            if (Unit == MeasureUnit.Piece)
            {
                yield return MeasureUnit.Piece;
                yield break;
            }

            if (Unit.IsVolume())
            {
                yield return MeasureUnit.Deciliter;
                yield return MeasureUnit.Milliliter;
                yield return MeasureUnit.Tablespoon;
                yield return MeasureUnit.Teaspoon;
                yield return MeasureUnit.Pinch;
            }
        }
    }

    // ----- Visning -----

    // Väljer automatiskt ett lagom mått. 3 g olja blir "0,6 tsk",
    // 180 g blir "2 dl" - i stället för "0,02 dl" respektive "36 tsk".
    public string FormatAmount(double grams)
    {
        if (Unit == MeasureUnit.Gram)
            return $"{Math.Round(grams)} g";

        if (Unit == MeasureUnit.Piece)
            return $"ca {FromGrams(grams, MeasureUnit.Piece):0.#} st ({Math.Round(grams)} g)";

        var unit = PickVolumeUnit(grams);
        double amount = FromGrams(grams, unit);

        return $"{amount:0.##} {unit.Label()} ({Math.Round(grams)} g)";
    }

    private MeasureUnit PickVolumeUnit(double grams)
    {
        double ml = grams / SafeDensity;

        if (ml >= 50) return MeasureUnit.Deciliter;
        if (ml >= 10) return MeasureUnit.Tablespoon;
        if (ml >= 2) return MeasureUnit.Teaspoon;
        return MeasureUnit.Pinch;
    }

    // ----- Hjälp till formulären -----

    public static double DefaultDensity(FoodCategory category) => category switch
    {
        FoodCategory.Fat => 0.92,
        FoodCategory.Grain => 0.6,
        FoodCategory.Drink => 1,
        FoodCategory.Dairy => 1.03,
        _ => 1
    };

    public static string GroupLabel(SubstitutionGroup group) => group switch
    {
        SubstitutionGroup.MincedBase => "Färs och baljväxter",
        SubstitutionGroup.MilkDrink => "Mjölk och dryck",
        SubstitutionGroup.HardCheese => "Hårdost",
        SubstitutionGroup.Flour => "Mjöl",
        SubstitutionGroup.PastaRice => "Pasta och ris",
        SubstitutionGroup.CookingFat => "Matfett",
        SubstitutionGroup.Sweetener => "Sötning",
        SubstitutionGroup.Onion => "Lök",
        SubstitutionGroup.TomatoBase => "Tomatbas",
        _ => "Inga utbyten"
    };
}