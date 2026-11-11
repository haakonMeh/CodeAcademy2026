using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm.Data;

/// <summary>
/// Kaffebaren slik den ser ut når testene starter: 6 brennerier, 15 bønner og 100 ordrer.
/// </summary>
/// <remarks>
/// Alt er deterministisk (<c>new Random(2026)</c> og faste datoer). Samme data på hver maskin
/// og ved hver kjøring, slik at tallene i loggen din er de samme som naboens.
///
/// Brenneriene finnes, men bønnene og salgstallene er oppdiktet.
///
/// De fire første ordrene er håndlaget og har de samme kundene som i samling 3. Testene
/// bruker dem fordi de trenger noe helt bestemt: Ada har Tim Wendelboe-bønner (oppgave 3A),
/// Jonas står og venter (oppgave 4A).
/// </remarks>
public static class SeedData
{
    public static readonly Guid AdaOrderId = Guid.Parse("0de50001-0000-4000-8000-000000000001");
    public static readonly Guid KariOrderId = Guid.Parse("0de50001-0000-4000-8000-000000000002");
    public static readonly Guid JonasOrderId = Guid.Parse("0de50001-0000-4000-8000-000000000003");
    public static readonly Guid IngridOrderId = Guid.Parse("0de50001-0000-4000-8000-000000000004");

    public const int OrderCount = 100;

    /// <summary>Menyen fra samling 3, uten Chai Latte (den har ingen kaffebønner).</summary>
    public static readonly IReadOnlyList<(string Name, decimal Price, bool HasMilk)> Menu =
    [
        ("Filterkaffe", 39, false),
        ("Espresso", 35, false),
        ("Dobbel espresso", 45, false),
        ("Americano", 42, false),
        ("Cortado", 47, true),
        ("Cappuccino", 49, true),
        ("Kaffe Latte", 52, true),
        ("Flat White", 55, true),
        ("Mocha", 59, true)
    ];

    private static readonly DateTimeOffset LastDay = new(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);

    public static void Populate(KaffebarDbContext db)
    {
        var roasteries = Roasteries();
        var beans = Beans(roasteries);

        db.Roasteries.AddRange(roasteries);
        db.Beans.AddRange(beans);
        db.Orders.AddRange(HandmadeOrders(beans));
        db.Orders.AddRange(GeneratedOrders(beans));
        db.SaveChanges();
    }

    private static List<Roastery> Roasteries() =>
    [
        new() { Name = "Tim Wendelboe", City = "Oslo" },
        new() { Name = "Kaffa", City = "Oslo" },
        new() { Name = "Fuglen", City = "Oslo" },
        new() { Name = "Supreme Roastworks", City = "Oslo" },
        new() { Name = "Solberg & Hansen", City = "Oslo" },
        new() { Name = "Jacobsen & Svart", City = "Trondheim" }
    ];

    private static List<Bean> Beans(List<Roastery> r) =>
    [
        new() { Name = "Huila", Origin = "Colombia", Process = "Washed", Roastery = r[0] },
        new() { Name = "Nyeri", Origin = "Kenya", Process = "Washed", Roastery = r[0] },
        new() { Name = "Guji", Origin = "Etiopia", Process = "Natural", Roastery = r[0] },
        new() { Name = "Kirinyaga", Origin = "Kenya", Process = "Washed", Roastery = r[1] },
        new() { Name = "Cerrado", Origin = "Brasil", Process = "Natural", Roastery = r[1] },
        new() { Name = "Yirgacheffe", Origin = "Etiopia", Process = "Washed", Roastery = r[2] },
        new() { Name = "Tarrazú", Origin = "Costa Rica", Process = "Honey", Roastery = r[2] },
        new() { Name = "Huehuetenango", Origin = "Guatemala", Process = "Washed", Roastery = r[3] },
        new() { Name = "Kivu", Origin = "Rwanda", Process = "Washed", Roastery = r[3] },
        new() { Name = "Santa Barbara", Origin = "Honduras", Process = "Washed", Roastery = r[3] },
        new() { Name = "Mogiana", Origin = "Brasil", Process = "Pulped natural", Roastery = r[4] },
        new() { Name = "Sidamo", Origin = "Etiopia", Process = "Natural", Roastery = r[4] },
        new() { Name = "Cauca", Origin = "Colombia", Process = "Washed", Roastery = r[4] },
        new() { Name = "Kayanza", Origin = "Burundi", Process = "Washed", Roastery = r[5] },
        new() { Name = "Nariño", Origin = "Colombia", Process = "Washed", Roastery = r[5] }
    ];

    private static IEnumerable<Order> HandmadeOrders(List<Bean> beans)
    {
        var today = LastDay.AddHours(8);

        yield return NewOrder(AdaOrderId, "Ada", OrderStatus.Ready, today.AddMinutes(-12),
            Line("Kaffe Latte", CoffeeSize.Medium, MilkType.Oat, false, 1, beans[0])); // Tim Wendelboe

        yield return NewOrder(KariOrderId, "Kari", OrderStatus.Brewing, today.AddMinutes(-6),
            Line("Cappuccino", CoffeeSize.Large, MilkType.Whole, true, 2, beans[3])); // Kaffa

        yield return NewOrder(JonasOrderId, "Jonas", OrderStatus.Ready, today.AddMinutes(-3),
            Line("Espresso", CoffeeSize.Small, null, false, 1, beans[7])); // Supreme Roastworks

        yield return NewOrder(IngridOrderId, "Ingrid", OrderStatus.Pending, today.AddMinutes(-1),
            Line("Mocha", CoffeeSize.Medium, MilkType.Soy, false, 3, beans[5]), // Fuglen
            Line("Filterkaffe", CoffeeSize.Large, null, false, 1, beans[1])); // Tim Wendelboe
    }

    private static IEnumerable<Order> GeneratedOrders(List<Bean> beans)
    {
        string[] names =
        [
            "Ola", "Nora", "Emil", "Sofie", "Jakob", "Emma", "Filip", "Maja", "Lukas", "Ella",
            "Oskar", "Olivia", "Henrik", "Ingeborg", "Aksel", "Frida", "Magnus", "Tiril", "Sander", "Hedda"
        ];
        MilkType[] milks = [MilkType.Whole, MilkType.Skimmed, MilkType.Oat, MilkType.Soy];
        var random = new Random(2026);
        var firstDay = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero); // mars–august: seks hele måneder
        var days = (int)(LastDay - firstDay).TotalDays;

        for (var i = 5; i <= OrderCount; i++)
        {
            var createdAt = firstDay
                .AddDays(random.Next(days))
                .AddHours(7 + random.Next(10))
                .AddMinutes(random.Next(60));

            var lines = Enumerable.Range(0, random.Next(1, 5)).Select(_ =>
            {
                var drink = Menu[random.Next(Menu.Count)];
                return Line(
                    drink.Name,
                    (CoffeeSize)random.Next(3),
                    drink.HasMilk ? milks[random.Next(milks.Length)] : null,
                    random.Next(4) == 0,
                    random.Next(1, 4),
                    beans[random.Next(beans.Count)]);
            }).ToArray();

            // Alt som er eldre enn i dag er hentet. Historikken er det som gjør det morsomt.
            yield return NewOrder(OrderId(i), names[random.Next(names.Length)], OrderStatus.PickedUp, createdAt, lines);
        }
    }

    public static Guid OrderId(int number) => Guid.Parse($"0de50001-0000-4000-8000-{number:x12}");

    public static decimal PriceFor(string drinkName, CoffeeSize size, bool extraShot)
    {
        var basePrice = Menu.Single(m => m.Name == drinkName).Price;
        var sizeAdjustment = size switch
        {
            CoffeeSize.Small => -5m,
            CoffeeSize.Large => 10m,
            _ => 0m
        };
        return basePrice + sizeAdjustment + (extraShot ? 8m : 0m);
    }

    private static OrderLine Line(string drink, CoffeeSize size, MilkType? milk, bool extraShot, int quantity, Bean bean) =>
        new()
        {
            DrinkName = drink,
            Size = size,
            MilkType = milk,
            ExtraShot = extraShot,
            Quantity = quantity,
            UnitPrice = PriceFor(drink, size, extraShot),
            Bean = bean
        };

    private static Order NewOrder(Guid id, string customer, OrderStatus status, DateTimeOffset createdAt, params OrderLine[] lines)
    {
        var order = new Order
        {
            Id = id,
            CustomerName = customer,
            Status = status,
            CreatedAt = createdAt,
            Lines = [.. lines],
            TotalPrice = lines.Sum(l => l.UnitPrice * l.Quantity)
        };

        // Pending → Brewing → Ready → PickedUp, ett steg for hvert trinn ordren har nådd.
        OrderStatus[] flow = [OrderStatus.Pending, OrderStatus.Brewing, OrderStatus.Ready, OrderStatus.PickedUp];
        int[] minutesAfter = [0, 2, 5, 9];
        for (var step = 0; step <= (int)status; step++)
        {
            order.StatusHistory.Add(new StatusChange
            {
                Status = flow[step],
                ChangedAt = createdAt.AddMinutes(minutesAfter[step])
            });
        }

        return order;
    }
}
