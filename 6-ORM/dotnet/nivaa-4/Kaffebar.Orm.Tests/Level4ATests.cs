using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 4: Kollisjonen. Oppgave 4A.</summary>
public class Level4ATests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level4A_TwoBaristas_OnlyOneCanHandOutTheCoffee()
    {
        // To baristaer, to contexter, samme ordre. Begge ser at Jonas sin espresso er klar.
        await using var db1 = fixture.CreateContext(s => output.WriteLine("[barista 1] " + s));
        await using var db2 = fixture.CreateContext(s => output.WriteLine("[barista 2] " + s));
        var barista1 = new KaffebarRepository(db1);
        var barista2 = new KaffebarRepository(db2);

        var order1 = await barista1.GetOrderAsync(SeedData.JonasOrderId);
        var order2 = await barista2.GetOrderAsync(SeedData.JonasOrderId);

        var first = await barista1.MarkAsPickedUpAsync(order1!);
        var second = await barista2.MarkAsPickedUpAsync(order2!);

        await using var fresh = fixture.CreateContext();
        var pickups = await fresh.StatusChanges
            .CountAsync(s => s.OrderId == SeedData.JonasOrderId && s.Status == OrderStatus.PickedUp);
        output.WriteLine($"\nBarista 1: {first}. Barista 2: {second}. Jonas har {pickups} PickedUp-rad(er) i historikken.");

        Assert.Equal(PickupResult.PickedUp, first);
        Assert.Equal(PickupResult.Conflict, second);
        Assert.Equal(1, pickups);

        // Etter konflikten skal barista 2 sin context være ryddet: ingenting igjen å lagre.
        Assert.False(db2.ChangeTracker.HasChanges(),
            "Barista 2 sin context har fortsatt ulagrede endringer. Neste SaveChanges vil prøve igjen:\n" +
            db2.ChangeTracker.DebugView.ShortView);
    }

    [Fact]
    public async Task Level4A_OrderThatIsNotReady_CannotBePickedUp()
    {
        await using var db = fixture.CreateContext(output.WriteLine);
        var repository = new KaffebarRepository(db);

        var ingrid = await repository.GetOrderAsync(SeedData.IngridOrderId);

        Assert.Equal(PickupResult.NotReady, await repository.MarkAsPickedUpAsync(ingrid!));
    }
}
