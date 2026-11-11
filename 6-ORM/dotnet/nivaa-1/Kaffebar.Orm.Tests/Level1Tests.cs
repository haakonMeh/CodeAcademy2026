using Kaffebar.Orm.Data;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 1: Den naive kvitteringen. Oppgave 1A og 1B.</summary>
public class Level1Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    private async Task<int> LinesInDatabaseAsync()
    {
        await using var db = fixture.CreateContext();
        return await db.OrderLines.CountAsync();
    }

    [Fact]
    public async Task Level1A_NaiveQuery_ReturnsOrdersWithoutLines()
    {
        output.WriteLine($"Kjørt mot {fixture.Runtime}.");

        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var orders = await new KaffebarRepository(db).GetAllOrdersAsync();
        output.Heading("Kvitteringene");
        Receipts.Write(orders, output.WriteLine);

        output.Heading("Resultat");
        output.Facts(
            ("Ordrer", orders.Count),
            ("Linjer på kvitteringene", orders.Sum(o => o.Lines.Count)),
            ("Linjer i databasen", await LinesInDatabaseAsync()));
        output.Commands(counter);

        Assert.Equal(SeedData.OrderCount, orders.Count);
        // Dette er fella: ingen feil, ingen advarsel. Bare kvitteringer uten kaffe.
        Assert.All(orders, order => Assert.Empty(order.Lines));
    }

    [Fact]
    public async Task Level1B_LazyLoading_FillsTheReceipts_OneQueryAtATime()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter, lazyLoading: true);

        var orders = await new KaffebarRepository(db).GetAllOrdersAsync();
        output.Heading("Kvitteringene");
        Receipts.Write(orders, output.WriteLine);

        output.Heading("Resultat");
        output.Facts(
            ("Ordrer", orders.Count),
            ("Linjer på kvitteringene", orders.Sum(o => o.Lines.Count)),
            ("Linjer i databasen", await LinesInDatabaseAsync()));
        output.Commands(counter);

        Assert.All(orders, order => Assert.NotEmpty(order.Lines));
    }
}
