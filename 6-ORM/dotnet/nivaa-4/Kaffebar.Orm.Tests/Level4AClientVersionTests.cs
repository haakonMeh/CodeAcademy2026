using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using static Kaffebar.Orm.Tests.Print;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 4: Kollisjonen mellom to kall. Oppgave 4A, del 2.</summary>
public class Level4AClientVersionTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level4A_ClientWithAnOldVersion_GetsAConflict()
    {
        // Versjonen klienten fikk da den hentet ordren.
        var versionTheClientSaw = await fixture.ReadVersionAsync(SeedData.AdaOrderId);

        // Noen andre endrer ordren før baristaen rekker å trykke «Hentet». Den er fortsatt Ready.
        await using (var other = fixture.CreateContext())
        {
            (await other.Orders.FindAsync(SeedData.AdaOrderId))!.TotalPrice = 60m;
            await other.SaveChangesAsync();
        }
        output.Heading("Før baristaen trykker «Hentet»");
        output.Facts(
            ("Versjon klienten så", versionTheClientSaw),
            ("Noen andre lagret Ada, TotalPrice", Kr(60m)),
            ("Versjon i databasen nå", await fixture.ReadVersionAsync(SeedData.AdaOrderId)));
        output.WriteLine("");

        await using var db = fixture.CreateContext(output.WriteLine);
        var result = await new KaffebarRepository(db).MarkAsPickedUpAsync(SeedData.AdaOrderId, versionTheClientSaw);

        await using var fresh = fixture.CreateContext();
        var ada = await fresh.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == SeedData.AdaOrderId);
        output.Heading("Resultat");
        output.Facts(
            ("MarkAsPickedUpAsync", result),
            ("Ada, Status i databasen", ada.Status),
            ("Ada, PickedUp-rader i historikken", ada.StatusHistory.Count(s => s.Status == OrderStatus.PickedUp)));
        output.Tracked(db);

        Assert.Equal(PickupResult.Conflict, result);
        Assert.Equal(OrderStatus.Ready, ada.Status);
        Assert.DoesNotContain(ada.StatusHistory, s => s.Status == OrderStatus.PickedUp);
        Assert.False(db.ChangeTracker.HasChanges(), "Contexten har ulagrede endringer:\n" + db.ChangeTracker.DebugView.ShortView);
    }

    [Fact]
    public async Task Level4A_ClientWithTheCurrentVersion_PicksUp()
    {
        var version = await fixture.ReadVersionAsync(SeedData.JonasOrderId);

        await using var db = fixture.CreateContext(output.WriteLine);
        var result = await new KaffebarRepository(db).MarkAsPickedUpAsync(SeedData.JonasOrderId, version);

        output.Heading("Resultat");
        output.Facts(("Versjon klienten så", version), ("MarkAsPickedUpAsync", result));

        Assert.Equal(PickupResult.PickedUp, result);
    }
}
