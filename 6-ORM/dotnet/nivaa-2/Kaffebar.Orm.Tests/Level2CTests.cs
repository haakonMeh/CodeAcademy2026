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
        await using (var db = fixture.CreateContext(output.WriteLine))
        {
            var beans = await db.Beans.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync();
            orderId = await new KaffebarRepository(db).RegisterOrderAsync("Kari Kassatest",
            [
                new NewLine("Espresso", CoffeeSize.Medium, 2, 55m, beans[0]),
                new NewLine("Cortado", CoffeeSize.Medium, 1, 60m, beans[1])
            ]);
        }

        await using var fresh = fixture.CreateContext();
        var saved = await fresh.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == orderId);
        output.WriteLine($"\nI databasen: {saved.Lines.Count} linjer, totalpris {saved.TotalPrice} kr.");

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
            mochaId = await read.OrderLines
                .Where(l => l.OrderId == SeedData.IngridOrderId && l.DrinkName == "Mocha")
                .Select(l => l.Id)
                .SingleAsync();
        }

        await using (var db = fixture.CreateContext(output.WriteLine))
        {
            await new KaffebarRepository(db).RemoveLineAsync(SeedData.IngridOrderId, mochaId);
        }

        await using var fresh = fixture.CreateContext();
        var ingrid = await fresh.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == SeedData.IngridOrderId);
        output.WriteLine($"\nI databasen: {ingrid.Lines.Count} linje(r), totalpris {ingrid.TotalPrice} kr.");

        Assert.True(ingrid.Lines.All(l => l.Id != mochaId),
            "Linjen ligger fortsatt i databasen, og det kom ingen feilmelding. Hva så RemoveLineAsync da den lette etter linjen?");
        Assert.Equal(49m, ingrid.TotalPrice);
    }
}
