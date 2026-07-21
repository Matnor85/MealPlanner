namespace Domain.Entities;

// Råvaror i samma grupp kan ersätta varandra i ett recept, vikt för vikt.
// Grupperna är rollbaserade, inte biologiska - nötfärs och linser hör ihop
// för att de fyller samma funktion i en gryta.
public enum SubstitutionGroup
{
    None = 0,
    MincedBase = 1,    // nötfärs, fläskfärs, sojafärs, linser, bönor
    MilkDrink = 2,     // mjölk, havredryck, sojadryck
    HardCheese = 3,    // parmesan, västerbottensost
    Flour = 4,         // vetemjöl, dinkelmjöl
    PastaRice = 5,     // spaghetti, pasta, ris
    CookingFat = 6,    // olivolja, rapsolja, smör
    Sweetener = 7,     // strösocker, honung, sirap
    Onion = 8,         // gul lök, rödlök, schalottenlök
    TomatoBase = 9     // krossade tomater, passerade tomater
}