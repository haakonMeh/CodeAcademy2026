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

        output.WriteLine($"\n{orders} ordrer, {lines} linjer, {history} statusendringer.");
        output.WriteLine($"Databasen sendte {counter.RowsRead} rader i {counter.SelectCount} spørring(er).");

        Assert.All(result, order => Assert.NotEmpty(order.StatusHistory));
        Assert.All(result, order => Assert.NotEmpty(order.Lines));
        Assert.True(counter.RowsRead <= orders + lines + history,
            $"{counter.RowsRead} rader for {orders + lines + history} rader med data. Hvor kommer resten fra?");
    }
}
