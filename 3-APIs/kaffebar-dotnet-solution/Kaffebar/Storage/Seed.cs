using Kaffebar.Models;

namespace Kaffebar.Storage;

/// <summary>
/// Menyen og startordrene.
/// </summary>
/// <remarks>
/// Faste uuid-er, ikke <c>Guid.NewGuid()</c> som i starteren. En deltaker skal kunne
/// skrive den samme <c>coffeeId</c>-en i et curl-kall i dag og i morgen, og slides skal
/// kunne vise ekte id-er uten at de blir feil ved neste omstart.
///
/// Nøyaktig de samme ti varene, prisene og id-ene som
/// 4-Frontend/kaffebar-api/src/lib/seed.ts — og som Java-fasiten. Diff
/// <c>GET /menu</c> fra alle tre; de skal være like.
/// </remarks>
public static class Seed
{
    public static readonly IReadOnlyList<Coffee> Menu =
    [
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000001"), "Filterkaffe", 39),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000002"), "Espresso", 35),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000003"), "Dobbel espresso", 45),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000004"), "Americano", 42),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000005"), "Cortado", 47),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000006"), "Cappuccino", 49),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000007"), "Kaffe Latte", 52),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000008"), "Flat White", 55),
        new(Guid.Parse("c0ffee01-0000-4000-8000-000000000009"), "Chai Latte", 54),
        new(Guid.Parse("c0ffee01-0000-4000-8000-00000000000a"), "Mocha", 59)
    ];

    /// <summary>
    /// Fire ordrer i ulike statuser, slik at både kundeskjermen og baristakøen i
    /// samling 4 viser noe med én gang. Tidsstemplene settes relativt til oppstart, så
    /// «for 2 minutter siden» alltid stemmer.
    /// </summary>
    public static IEnumerable<Order> Orders(DateTimeOffset now) =>
    [
        new(Guid.Parse("0de50001-0000-4000-8000-000000000001"),
            Guid.Parse("c0ffee01-0000-4000-8000-000000000007"), "Kaffe Latte", "Ada",
            CoffeeSize.Medium, 1, OrderStatus.Ready,
            now.AddMinutes(-12), now.AddMinutes(-4), MilkType.Oat, false),

        new(Guid.Parse("0de50001-0000-4000-8000-000000000002"),
            Guid.Parse("c0ffee01-0000-4000-8000-000000000006"), "Cappuccino", "Kari",
            CoffeeSize.Large, 2, OrderStatus.Brewing,
            now.AddMinutes(-6), now.AddMinutes(-2), MilkType.Whole, true),

        new(Guid.Parse("0de50001-0000-4000-8000-000000000003"),
            Guid.Parse("c0ffee01-0000-4000-8000-000000000002"), "Espresso", "Jonas",
            CoffeeSize.Small, 1, OrderStatus.Pending,
            now.AddMinutes(-3), now.AddMinutes(-3)),

        new(Guid.Parse("0de50001-0000-4000-8000-000000000004"),
            Guid.Parse("c0ffee01-0000-4000-8000-00000000000a"), "Mocha", "Ingrid",
            CoffeeSize.Medium, 3, OrderStatus.Pending,
            now.AddMinutes(-1), now.AddMinutes(-1), MilkType.Soy, false)
    ];
}
