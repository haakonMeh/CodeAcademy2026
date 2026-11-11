using Kaffebar.Orm.Data;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 2: Ta tilbake kontrollen. Oppgave 2A og 2B.</summary>
public class Level2Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level2A_Include_LoadsTheWholeReceiptInOneQuery()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var orders = await new KaffebarRepository(db).GetAllOrdersWithDetailsAsync();

        var lines = orders.SelectMany(o => o.Lines).ToList();
        output.Heading("Resultat");
        output.Facts(
            ("Ordrer", orders.Count),
            ("Linjer", lines.Count),
            ("Linjer med bønne og brenneri", lines.Count(l => l.Bean?.Roastery is not null)));
        output.Commands(counter);
        output.Tracked(db);

        Assert.All(orders, order => Assert.NotEmpty(order.Lines));
        Assert.All(orders.SelectMany(o => o.Lines), line => Assert.NotNull(line.Bean.Roastery));
        Assert.Equal(1, counter.SelectCount);
    }

    [Fact]
    public async Task Level2A_Projection_LoadsTheWholeReceiptInOneQuery()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var receipts = await new KaffebarRepository(db).GetReceiptsAsync();

        output.Heading("Resultat");
        output.Facts(
            ("Kvitteringer", receipts.Count),
            ("Linjer", receipts.Sum(r => r.Lines.Count)));
        output.Commands(counter);
        output.Tracked(db);

        Assert.Equal(SeedData.OrderCount, receipts.Count);
        Assert.All(receipts, receipt => Assert.NotEmpty(receipt.Lines));
        Assert.Equal(1, counter.SelectCount);
        // En projeksjon gir ingen entiteter, og derfor ingenting å spore.
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Level2B_TwoCollections_DoNotMultiplyTheRows()
    {
        int orders, lines, history;
        await using (var count = fixture.CreateContext())
        {
            orders = await count.Orders.CountAsync();
            lines = await count.OrderLines.CountAsync();
            history = await count.StatusChanges.CountAsync();
        }

        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var result = await new KaffebarRepository(db).GetOrdersWithLinesAndHistoryAsync();

        output.Heading("Dataene");
        output.Facts(
            ("Ordrer", orders),
            ("Linjer", lines),
            ("Statusendringer", history),
            ("Rader med data totalt", orders + lines + history));
        output.Commands(counter);

        Assert.All(result, order => Assert.NotEmpty(order.StatusHistory));
        Assert.All(result, order => Assert.NotEmpty(order.Lines));
        Assert.True(counter.RowsRead <= orders + lines + history,
            $"Databasen sendte {counter.RowsRead} rader for {orders + lines + history} rader med data.");
    }
}
