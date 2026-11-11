using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 3: Masseoppdatering. Oppgave 3C (bonus).</summary>
public class Level3CTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level3C_StaleReadyOrders_ArePickedUp_AndTheHistoryIsKept()
    {
        var cutoff = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var counter = new QueryCounter();

        await using (var before = fixture.CreateContext())
        {
            var stale = await before.Orders
                .Where(o => o.Status == OrderStatus.Ready && o.CreatedAt < cutoff)
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.CustomerName)
                .ToListAsync();
            output.Heading("Før");
            output.Facts(($"Ready-ordrer bestilt før {cutoff:yyyy-MM-dd}", string.Join(", ", stale)));
            output.WriteLine("");
        }

        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            var updated = await new KaffebarRepository(db).PickUpStaleOrdersAsync(cutoff);
            output.Heading("Resultat");
            output.Facts(("Ordrer oppdatert", updated));
            output.Commands(counter);
            output.Tracked(db);
            Assert.Equal(2, updated); // Ada og Jonas
        }

        await using var fresh = fixture.CreateContext();
        var ada = await fresh.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == SeedData.AdaOrderId);
        var stillReady = await fresh.Orders.CountAsync(o => o.Status == OrderStatus.Ready && o.CreatedAt < cutoff);
        output.Heading("I databasen etterpå");
        output.Facts(
            ($"Ready-ordrer bestilt før {cutoff:yyyy-MM-dd}", stillReady),
            ("Ada, Status", ada.Status),
            ("Ada, StatusHistory", string.Join(" → ", ada.StatusHistory.OrderBy(s => s.ChangedAt).Select(s => s.Status))));

        Assert.Equal(0, stillReady);
        Assert.Equal(OrderStatus.PickedUp, ada.StatusHistory.MaxBy(s => s.ChangedAt)!.Status);
    }
}
