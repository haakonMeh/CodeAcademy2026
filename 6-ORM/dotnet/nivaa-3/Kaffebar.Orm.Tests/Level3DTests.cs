using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Xunit.Abstractions;
using static Kaffebar.Orm.Tests.Print;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 3: Den motsatte fellen. Oppgave 3D (bonus).</summary>
public class Level3DTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    private async Task<Order> ReadOrderAndCloseTheContextAsync(Guid orderId)
    {
        await using var db = fixture.CreateContext();
        return (await new KaffebarRepository(db).GetOrderAsync(orderId))!;
    }

    [Fact]
    public async Task Level3D_ChangeOnADetachedOrder_IsSaved()
    {
        // Kassa-appen henter ordren, og contexten forsvinner …
        var order = await ReadOrderAndCloseTheContextAsync(SeedData.KariOrderId);
        var statusRead = order.Status;

        // … baristaen markerer den som klar, og kassa-appen sender den inn igjen.
        order.Status = OrderStatus.Ready;
        output.Heading("Kassa-appen, Kari");
        output.Facts(("Status lest", statusRead), ("Status sendt inn", order.Status));
        output.WriteLine("");

        var counter = new QueryCounter();
        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            await new KaffebarRepository(db).SaveOrderAsync(order);
        }
        output.Commands(counter, "Kommandoer fra SaveOrderAsync");

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.FindAsync(SeedData.KariOrderId);
        output.Heading("I databasen etterpå, Kari");
        output.Facts(("Status", saved!.Status));

        Assert.Equal(OrderStatus.Ready, saved.Status);
    }

    [Fact]
    public async Task Level3D_SavingADetachedOrder_DoesNotOverwriteWhatOthersChanged()
    {
        var order = await ReadOrderAndCloseTheContextAsync(SeedData.AdaOrderId);
        output.Heading("Kassa-appen leste Ada");
        output.Facts(("Status", order.Status), ("TotalPrice", Kr(order.TotalPrice)));

        // Mens kassa-appen har ordren, retter noen andre prisen i databasen.
        await using (var other = fixture.CreateContext())
        {
            (await other.Orders.FindAsync(SeedData.AdaOrderId))!.TotalPrice = 999m;
            await other.SaveChangesAsync();
        }
        output.Heading("Noen andre lagret Ada");
        output.Facts(("TotalPrice", Kr(999m)));

        // Kassa-appen endret bare statusen, og vet ingenting om den nye prisen.
        order.Status = OrderStatus.PickedUp;
        output.Heading("Kassa-appen sender inn Ada");
        output.Facts(("Status", order.Status), ("TotalPrice", Kr(order.TotalPrice)));
        output.WriteLine("");

        var counter = new QueryCounter();
        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            await new KaffebarRepository(db).SaveOrderAsync(order);
        }
        output.Commands(counter, "Kommandoer fra SaveOrderAsync");

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.FindAsync(SeedData.AdaOrderId);
        output.Heading("I databasen etterpå");
        output.Facts(("Status", saved!.Status), ("TotalPrice", Kr(saved.TotalPrice)));

        Assert.Equal(OrderStatus.PickedUp, saved.Status);
        Assert.Equal(999m, saved.TotalPrice);
    }
}
