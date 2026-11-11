using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Xunit.Abstractions;

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

        // … baristaen markerer den som klar, og kassa-appen sender den inn igjen.
        order.Status = OrderStatus.Ready;
        await using (var db = fixture.CreateContext(output.WriteLine))
        {
            await new KaffebarRepository(db).SaveOrderAsync(order);
        }

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.FindAsync(SeedData.KariOrderId);
        Assert.Equal(OrderStatus.Ready, saved!.Status);
    }

    [Fact]
    public async Task Level3D_SavingADetachedOrder_DoesNotOverwriteWhatOthersChanged()
    {
        var order = await ReadOrderAndCloseTheContextAsync(SeedData.AdaOrderId);

        // Mens kassa-appen har ordren, retter noen andre prisen i databasen.
        await using (var other = fixture.CreateContext())
        {
            (await other.Orders.FindAsync(SeedData.AdaOrderId))!.TotalPrice = 999m;
            await other.SaveChangesAsync();
        }

        // Kassa-appen endret bare statusen, og vet ingenting om den nye prisen.
        order.Status = OrderStatus.PickedUp;
        await using (var db = fixture.CreateContext(output.WriteLine))
        {
            await new KaffebarRepository(db).SaveOrderAsync(order);
        }

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.FindAsync(SeedData.AdaOrderId);
        Assert.Equal(OrderStatus.PickedUp, saved!.Status);
        Assert.Equal(999m, saved.TotalPrice);
    }
}
