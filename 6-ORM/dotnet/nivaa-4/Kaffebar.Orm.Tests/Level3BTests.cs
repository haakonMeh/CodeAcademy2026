using System.Text.Json;
using Kaffebar.Orm.Data;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 3: Entiteter ut av datalaget. Oppgave 3B.</summary>
public class Level3BTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level3B_OrderJson_HasTheFormatTheCashRegisterExpects()
    {
        await using var db = fixture.CreateContext(output.WriteLine);

        var json = await new KaffebarRepository(db).GetOrderJsonAsync(SeedData.IngridOrderId);
        output.WriteLine($"\n{json}");

        var order = JsonDocument.Parse(json!).RootElement;
        var lines = order.GetProperty("lines").EnumerateArray().ToList();
        Assert.True(lines.Count == 2, $"Ingrid har 2 linjer, men JSON-en har {lines.Count}. Ble de lastet?");

        Assert.Equal(SeedData.IngridOrderId, order.GetProperty("id").GetGuid());
        Assert.Equal("Ingrid", order.GetProperty("customerName").GetString());
        Assert.Equal("Pending", order.GetProperty("status").GetString());
        Assert.Equal(226m, order.GetProperty("totalPrice").GetDecimal());

        var mocha = lines.Single(l => l.GetProperty("quantity").GetInt32() == 3);
        Assert.Equal("Yirgacheffe", mocha.GetProperty("bean").GetString());
        Assert.Equal("Fuglen", mocha.GetProperty("roastery").GetString());
        Assert.Equal(59m, mocha.GetProperty("unitPrice").GetDecimal());

        var history = order.GetProperty("statusHistory").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.Equal(["Pending"], history);
    }

    [Fact]
    public async Task Level3B_OrderJson_UsesFewQueries_AndLeavesNothingTracked()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var json = await new KaffebarRepository(db).GetOrderJsonAsync(SeedData.JonasOrderId);

        output.WriteLine($"\n{counter.SelectCount} SELECT-spørring(er), {db.ChangeTracker.Entries().Count()} entiteter sporet.");
        Assert.NotNull(json);
        Assert.InRange(counter.SelectCount, 1, 3);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Level3B_UnknownOrder_GivesNull()
    {
        await using var db = fixture.CreateContext(output.WriteLine);

        Assert.Null(await new KaffebarRepository(db).GetOrderJsonAsync(Guid.NewGuid()));
    }
}
