namespace Domain.Entities;

// Hur råvaran naturligt mäts. Allt LAGRAS alltid i gram - detta styr
// bara hur mängden visas och matas in.
public enum MeasureUnit
{
    Gram = 0,
    Milliliter = 1,
    Deciliter = 2,
    Tablespoon = 3,   // msk
    Teaspoon = 4,     // tsk
    Pinch = 5,        // krm
    Piece = 6
}

public static class MeasureUnitExtensions
{
    // Alla volymmått är fasta multiplar av milliliter. Därför behöver
    // råvaran bara känna till sin densitet, inte varje mått för sig.
    public static double MilliliterFactor(this MeasureUnit unit) => unit switch
    {
        MeasureUnit.Milliliter => 1,
        MeasureUnit.Deciliter => 100,
        MeasureUnit.Tablespoon => 15,
        MeasureUnit.Teaspoon => 5,
        MeasureUnit.Pinch => 1,
        _ => 0   // inte ett volymmått
    };

    public static bool IsVolume(this MeasureUnit unit) => unit.MilliliterFactor() > 0;

    public static string Label(this MeasureUnit unit) => unit switch
    {
        MeasureUnit.Gram => "g",
        MeasureUnit.Milliliter => "ml",
        MeasureUnit.Deciliter => "dl",
        MeasureUnit.Tablespoon => "msk",
        MeasureUnit.Teaspoon => "tsk",
        MeasureUnit.Pinch => "krm",
        MeasureUnit.Piece => "st",
        _ => "g"
    };

    public static string LongLabel(this MeasureUnit unit) => unit switch
    {
        MeasureUnit.Gram => "Gram",
        MeasureUnit.Milliliter => "Milliliter",
        MeasureUnit.Deciliter => "Deciliter",
        MeasureUnit.Tablespoon => "Matsked",
        MeasureUnit.Teaspoon => "Tesked",
        MeasureUnit.Pinch => "Kryddmått",
        MeasureUnit.Piece => "Styck",
        _ => "Gram"
    };
}