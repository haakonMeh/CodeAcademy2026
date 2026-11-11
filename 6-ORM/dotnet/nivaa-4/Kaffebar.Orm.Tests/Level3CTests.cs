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

        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            var updated = await new KaffebarRepository(db).PickUpStaleOrdersAsync(cutoff);
            output.WriteLine($"\n{updated} ordrer oppdatert med {counter.CommandCount} kommando(er). " +
                             $"{counter.RowsRead} rader lest inn i minnet, {db.ChangeTracker.Entries().Count()} entiteter sporet.");
            Assert.Equal(2, updated); // Ada og Jonas
        }

        await using var fresh = fixture.CreateContext();
        Assert.False(await fresh.Orders.AnyAsync(o => o.Status == OrderStatus.Ready && o.CreatedAt < cutoff));

        var ada = await fresh.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == SeedData.AdaOrderId);
        Assert.Equal(OrderStatus.PickedUp, ada.StatusHistory.MaxBy(s => s.ChangedAt)!.Status);
    }
}
