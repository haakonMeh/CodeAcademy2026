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

        output.Heading("Resultat");
        output.Facts(
            ("Ordrer i fila", OrderCount),
            ("Linjer i fila", OrderCount * 2),
            ("Ordrer importert", imported),
            ("Tid", $"{watch.ElapsedMilliseconds} ms"));
        output.Commands(counter);
        output.Tracked(db);

        await using var fresh = fixture.CreateContext();
        Assert.Equal(OrderCount, imported);
        Assert.Equal(OrderCount, await fresh.Orders.CountAsync(o => o.CustomerName.StartsWith("Nettkunde")));
        Assert.Equal(OrderCount * 2, await fresh.OrderLines.CountAsync(l => l.Order.CustomerName.StartsWith("Nettkunde")));
        Assert.True(counter.CommandCount <= MaxCommands, $"{counter.CommandCount} kommandoer, maks {MaxCommands}.");
        Assert.True(tracked <= MaxTrackedEntities, $"{tracked} entiteter igjen i change trackeren, maks {MaxTrackedEntities}.");
    }

    [Fact]
    public async Task Level4C_ImportThatFails_SavesNothing()
    {
        var beans = await BeanIdsAsync();
        var orders = Orders("Halvferdig", 250, beans);

        // Den aller siste linjen i fila peker på en bønne som ikke finnes.
        var last = orders[^1];
        orders[^1] = last with { Lines = [last.Lines[0] with { BeanId = -1 }] };

        DbUpdateException error;
        await using (var db = fixture.CreateContext())
        {
            error = await Assert.ThrowsAsync<DbUpdateException>(() => new KaffebarRepository(db).ImportOrdersAsync(orders));
        }

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.CountAsync(o => o.CustomerName.StartsWith("Halvferdig"));
        output.Heading("Resultat");
        output.Facts(
            ("Ordrer i fila", orders.Count),
            ("BeanId på siste linje", -1),
            ("Feil", (error.InnerException ?? error).Message.Split('\n')[0]),
            ("Ordrer lagret i databasen", saved));
        Assert.Equal(0, saved);
    }
}
