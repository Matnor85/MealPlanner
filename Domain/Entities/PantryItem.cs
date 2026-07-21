namespace Domain.Entities;

// Hur bråttom det är med en vara. Styr både om datumet visas alls
// och vilken färg det får.
public enum ExpiryLevel
{
    None = 0,      // inget datum, eller mer än en vecka kvar - visas inte
    Soon = 1,      // 5-7 dagar kvar
    Warning = 2,   // 2-4 dagar kvar
    Critical = 3,  // i dag eller i morgon
    Expired = 4
}

// Vad som faktiskt finns hemma. En rad per råvara - mängder slås ihop.
public class PantryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FoodId { get; set; }
    public FoodItem? Food { get; set; }

    // Lagras i gram, precis som allt annat
    public double Grams { get; set; }

    // Valfritt bäst före-datum
    public DateOnly? BestBefore { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string DisplayAmount => Food?.FormatAmount(Grams) ?? $"{Math.Round(Grams)} g";

    public int? DaysLeft => BestBefore is DateOnly d
        ? d.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber
        : null;

    public ExpiryLevel Expiry => DaysLeft switch
    {
        null => ExpiryLevel.None,
        < 0 => ExpiryLevel.Expired,
        <= 1 => ExpiryLevel.Critical,
        <= 4 => ExpiryLevel.Warning,
        <= 7 => ExpiryLevel.Soon,
        _ => ExpiryLevel.None
    };

    // Datumet är ointressant så länge det är långt bort - det tar bara plats
    public bool ShowExpiry => Expiry != ExpiryLevel.None;

    public bool IsExpired => Expiry == ExpiryLevel.Expired;

    // Används av receptförslagen för märkningen "räddar mat"
    public bool IsExpiringSoon => Expiry is ExpiryLevel.Soon
                                          or ExpiryLevel.Warning
                                          or ExpiryLevel.Critical;

    public string ExpiryText => DaysLeft switch
    {
        null => "",
        < 0 => "utgången",
        0 => "i dag",
        1 => "i morgon",
        int d => $"{d} dagar"
    };

    public string ExpiryCss => Expiry switch
    {
        ExpiryLevel.Expired => "expiry expired",
        ExpiryLevel.Critical => "expiry critical",
        ExpiryLevel.Warning => "expiry warning",
        ExpiryLevel.Soon => "expiry soon",
        _ => "expiry"
    };
}