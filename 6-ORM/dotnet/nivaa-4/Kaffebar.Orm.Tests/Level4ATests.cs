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
        output.Heading("Begge baristaene leste Jonas");
        output.Facts(("Status", order1!.Status), ("Versjon (xmin)", await fixture.ReadVersionAsync(SeedData.JonasOrderId)));
        output.WriteLine("");

        var first = await barista1.MarkAsPickedUpAsync(order1);
        var versionAfterFirst = await fixture.ReadVersionAsync(SeedData.JonasOrderId);
        var second = await barista2.MarkAsPickedUpAsync(order2!);

        await using var fresh = fixture.CreateContext();
        var jonas = await fresh.Orders.FindAsync(SeedData.JonasOrderId);
        var pickups = await fresh.StatusChanges
            .CountAsync(s => s.OrderId == SeedData.JonasOrderId && s.Status == OrderStatus.PickedUp);
        output.Heading("Resultat");
        output.Facts(
            ("Barista 1", first),
            ("Versjon (xmin) etter barista 1", versionAfterFirst),
            ("Barista 2", second),
            ("Jonas, Status i databasen", jonas!.Status),
            ("Jonas, PickedUp-rader i historikken", pickups));
        output.Tracked(db2, "Change trackeren til barista 2");

        Assert.Equal(PickupResult.PickedUp, first);
        Assert.Equal(PickupResult.Conflict, second);
        Assert.Equal(1, pickups);

        // Etter konflikten skal barista 2 sin context være ryddet: ingenting igjen å lagre.
        Assert.False(db2.ChangeTracker.HasChanges(),
            "Barista 2 sin context har ulagrede endringer:\n" + db2.ChangeTracker.DebugView.ShortView);
    }

    [Fact]
    public async Task Level4A_OrderThatIsNotReady_CannotBePickedUp()
    {
        await using var db = fixture.CreateContext(output.WriteLine);
        var repository = new KaffebarRepository(db);

        var ingrid = await repository.GetOrderAsync(SeedData.IngridOrderId);
        var result = await repository.MarkAsPickedUpAsync(ingrid!);

        output.Heading("Resultat");
        output.Facts(("Ingrid, Status", ingrid!.Status), ("MarkAsPickedUpAsync", result));

        Assert.Equal(PickupResult.NotReady, result);
    }
}
