using System.Text.Json.Serialization;

namespace Kaffebar.Models;

/// <summary>
/// Livssyklusen til en bestilling: PENDING → BREWING → READY.
/// </summary>
/// <remarks>
/// Oppgave 3. Verdiene på tråden SKAL være store bokstaver, fordi det er det
/// kontrakten i 4-Frontend/kaffebar-api sier — og fordi Java-sporet og
/// TypeScript-fasiten gjør det samme.
///
/// C#-navnekonvensjonen er PascalCase, så i stedet for å døpe om medlemmene til
/// PENDING/BREWING/READY bruker vi <see cref="JsonStringEnumMemberNameAttribute"/>.
/// Da er koden idiomatisk C# og JSON-en idiomatisk for kontrakten. Attributtet er
/// nytt i .NET 9 og påvirker både serialisering og den genererte
/// OpenAPI-spesifikasjonen.
///
/// Uten <c>JsonStringEnumConverter</c> (registrert i Program.cs) ville disse blitt
/// til 0, 1 og 2 i JSON. Det er den vanligste enkeltfeilen i .NET-sporet.
/// </remarks>
public enum OrderStatus
{
    [JsonStringEnumMemberName("PENDING")] Pending,
    [JsonStringEnumMemberName("BREWING")] Brewing,
    [JsonStringEnumMemberName("READY")] Ready
}

/// <summary>Størrelsen på kaffen.</summary>
public enum CoffeeSize
{
    [JsonStringEnumMemberName("SMALL")] Small,
    [JsonStringEnumMemberName("MEDIUM")] Medium,
    [JsonStringEnumMemberName("LARGE")] Large
}

/// <summary>Melketypen. Valgfri — en espresso trenger ingen.</summary>
public enum MilkType
{
    [JsonStringEnumMemberName("WHOLE")] Whole,
    [JsonStringEnumMemberName("SKIMMED")] Skimmed,
    [JsonStringEnumMemberName("OAT")] Oat,
    [JsonStringEnumMemberName("SOY")] Soy
}
