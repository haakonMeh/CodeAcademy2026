using System.Text.Json.Serialization;

namespace Oppgave9;

/// <summary>
/// Oppgave 9 — polymorfisme. Kaffebaren har begynt å selge bakst.
/// </summary>
/// <remarks>
/// En ordrelinje er nå ENTEN en kaffedrikk ELLER et bakverk, og de to har ulike
/// attributter: kaffe har <c>MilkType</c>, bakst har <c>IsVegan</c>.
///
/// <c>[JsonPolymorphic]</c> på basetypen forteller System.Text.Json at det finnes
/// undertyper, og hvilket felt som avgjør hvilken. <c>[JsonDerivedType]</c> knytter en
/// verdi i det feltet til en konkret klasse.
///
/// Legg merke til at <c>type</c>-feltet IKKE er deklarert som en egenskap noe sted.
/// System.Text.Json skriver det selv ved serialisering og leser det selv ved
/// deserialisering. Legger du til en egen <c>Type</c>-egenskap, får du feltet to ganger
/// i JSON-en.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CoffeeItem), "coffee")]
[JsonDerivedType(typeof(PastryItem), "pastry")]
public abstract record OrderItem;

public sealed record CoffeeItem(
    Guid CoffeeId,
    CoffeeSize Size,
    MilkType? MilkType = null,
    bool? ExtraShot = null) : OrderItem;

public sealed record PastryItem(
    Guid PastryId,
    bool IsVegan,
    bool? Warmed = null) : OrderItem;

public sealed record CreateMixedOrderRequest(string CustomerName, IReadOnlyList<OrderItem> Items);

public sealed record MixedOrder(
    Guid OrderId,
    string CustomerName,
    IReadOnlyList<OrderItem> Items,
    OrderStatus Status,
    DateTimeOffset CreatedAt);

public enum OrderStatus
{
    [JsonStringEnumMemberName("PENDING")] Pending,
    [JsonStringEnumMemberName("BREWING")] Brewing,
    [JsonStringEnumMemberName("READY")] Ready
}

public enum CoffeeSize
{
    [JsonStringEnumMemberName("SMALL")] Small,
    [JsonStringEnumMemberName("MEDIUM")] Medium,
    [JsonStringEnumMemberName("LARGE")] Large
}

public enum MilkType
{
    [JsonStringEnumMemberName("WHOLE")] Whole,
    [JsonStringEnumMemberName("SKIMMED")] Skimmed,
    [JsonStringEnumMemberName("OAT")] Oat,
    [JsonStringEnumMemberName("SOY")] Soy
}
