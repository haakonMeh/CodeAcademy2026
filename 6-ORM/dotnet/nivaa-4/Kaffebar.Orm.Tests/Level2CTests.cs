using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 2: Kassa-appen skriver. Oppgave 2C.</summary>
public class Level2CTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level2C_RegisterOrder_SavesTheLinesAndTheTotalPrice()
    {
        Guid orderId;
        var counter = new QueryCounter();
        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            var beans = await db.Beans.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync();
            counter.Reset();
            orderId = await new KaffebarRepository(db).RegisterOrderAsync("Kari Kassatest",
            [
                new NewLine("Espresso", CoffeeSize.Medium, 2, 55m, beans[0]),
                new NewLine("Cortado", CoffeeSize.Medium, 1, 60m, beans[1])
            ]);
        }
        output.Commands(counter, "Kommandoer fra RegisterOrderAsync");

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == orderId);
        output.Heading("I databasen etterpå");
        output.Order(saved);

        Assert.Equal(2, saved.Lines.Count);
        Assert.Equal(170m, saved.TotalPrice);
    }

    [Fact]
    public async Task Level2C_RemoveLine_DeletesTheLineAndUpdatesTheTotalPrice()
    {
        // Ingrid: 3 × Mocha (177 kr) og én Filterkaffe (49 kr), til sammen 226 kr.
        int mochaId;
        await using (var read = fixture.CreateContext())
        {
            var before = await read.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == SeedData.IngridOrderId);
            mochaId = before.Lines.Single(l => l.DrinkName == "Mocha").Id;
            output.Heading("Ingrid før");
            output.Order(before);
            output.Facts(("Linjen som skal fjernes", mochaId));
            output.WriteLine("");
        }

        var counter = new QueryCounter();
        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            await new KaffebarRepository(db).RemoveLineAsync(SeedData.IngridOrderId, mochaId);
        }
        output.Commands(counter, "Kommandoer fra RemoveLineAsync");

        await using var fresh = fixture.CreateContext();
        var ingrid = await fresh.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == SeedData.IngridOrderId);
        output.Heading("Ingrid etterpå, i databasen");
        output.Order(ingrid);

        Assert.True(ingrid.Lines.All(l => l.Id != mochaId), $"Linje {mochaId} ligger fortsatt i databasen.");
        Assert.Equal(49m, ingrid.TotalPrice);
    }
}
