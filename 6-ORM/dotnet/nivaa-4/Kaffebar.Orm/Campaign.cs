namespace Kaffebar.Orm;

/// <summary>
/// Oppgave 3A: kampanjeregelen som ren funksjon. Ingen entiteter og ingen DbContext, bare
/// tall inn og tall ut. Den kan ikke mutere databasen, fordi den aldri ser den.
/// </summary>
public static class Campaign
{
    public const string RoasteryName = "Tim Wendelboe";
    public const decimal Discount = 0.15m;

    public static decimal Apply(decimal ordinaryPrice, bool eligible) =>
        eligible ? Math.Round(ordinaryPrice * (1 - Discount), 2) : ordinaryPrice;
}
