using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 4: Black Friday-importen. Oppgave 4C (bonus).</summary>
public class Level4CTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    private const int OrderCount = 1000;
    private const int MaxCommands = 100;
    private const int MaxTrackedEntities = 300;

    private async Task<List<int>> BeanIdsAsync()
    {
        await using var db = fixture.CreateContext();
        return await db.Beans.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync();
    }

    private static List<NewOrder> Orders(string prefix, int count, List<int> beans)
    {
        var start = new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);
        return Enumerable.Range(0, count)
            .Select(i => new NewOrder(
                $"{prefix} {i}",
                start.AddSeconds(i * 37),
                [
                    new NewLine("Espresso", CoffeeSize.Medium, 1, 55m, beans[i % beans.Count]),
                    new NewLine("Cortado", CoffeeSize.Medium, 2, 50m, beans[(i + 7) % beans.Count])
                ]))
            .ToList();
    }

    [Fact]
    public async Task Level4C_Import_UsesFewCommands_AndKeepsTheChangeTrackerSmall()
    {
        var orders = Orders("Nettkunde", OrderCount, await BeanIdsAsync());
        var counter = new QueryCounter();

        // Ingen SQL-logg her. Det ville blitt 3000 rader i testresultatet.
        await using var db = fixture.CreateContext(counter: counter);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var imported = await new KaffebarRepository(db).ImportOrdersAsync(orders);
        watch.Stop();
        var tracked = db.ChangeTracker.Entries().Count();

        output.WriteLine($"{imported} ordrer med {OrderCount * 2} linjer på {watch.ElapsedMilliseconds} ms: " +
                         $"{counter.CommandCount} kommandoer, {tracked} entiteter igjen i change trackeren.");

        await using var fresh = fixture.CreateContext();
        Assert.Equal(OrderCount, imported);
        Assert.Equal(OrderCount, await fresh.Orders.CountAsync(o => o.CustomerName.StartsWith("Nettkunde")));
        Assert.Equal(OrderCount * 2, await fresh.OrderLines.CountAsync(l => l.Order.CustomerName.StartsWith("Nettkunde")));
        Assert.True(counter.CommandCount <= MaxCommands, $"{counter.CommandCount} kommandoer. Hvor mange ganger kalles SaveChanges?");
        Assert.True(tracked <= MaxTrackedEntities, $"{tracked} entiteter ligger igjen i change trackeren.");
    }

    [Fact]
    public async Task Level4C_ImportThatFails_SavesNothing()
    {
        var beans = await BeanIdsAsync();
        var orders = Orders("Halvferdig", 250, beans);

        // Den aller siste linjen i fila peker på en bønne som ikke finnes.
        var last = orders[^1];
        orders[^1] = last with { Lines = [last.Lines[0] with { BeanId = -1 }] };

        await using (var db = fixture.CreateContext())
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => new KaffebarRepository(db).ImportOrdersAsync(orders));
        }

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.CountAsync(o => o.CustomerName.StartsWith("Halvferdig"));
        output.WriteLine($"{saved} av {orders.Count} ordrer ble liggende i databasen etter feilen.");
        Assert.Equal(0, saved);
    }
}
