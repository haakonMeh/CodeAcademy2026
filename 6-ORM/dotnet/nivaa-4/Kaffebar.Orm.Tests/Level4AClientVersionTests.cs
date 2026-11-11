using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 4: Kollisjonen mellom to kall. Oppgave 4A, del 2.</summary>
public class Level4AClientVersionTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    /// <summary>Versjonen klienten fikk da den hentet ordren. Rett fra databasen, så testen kompilerer før du har lagt til Version.</summary>
    private async Task<uint> ReadVersionAsync(Guid orderId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT xmin::text::bigint FROM orders WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", orderId);
        return (uint)(long)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task Level4A_ClientWithAnOldVersion_GetsAConflict()
    {
        var versionTheClientSaw = await ReadVersionAsync(SeedData.AdaOrderId);

        // Noen andre endrer ordren før baristaen rekker å trykke «Hentet». Den er fortsatt Ready.
        await using (var other = fixture.CreateContext())
        {
            (await other.Orders.FindAsync(SeedData.AdaOrderId))!.TotalPrice = 60m;
            await other.SaveChangesAsync();
        }

        await using var db = fixture.CreateContext(output.WriteLine);
        var result = await new KaffebarRepository(db).MarkAsPickedUpAsync(SeedData.AdaOrderId, versionTheClientSaw);

        await using var fresh = fixture.CreateContext();
        var ada = await fresh.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == SeedData.AdaOrderId);
        output.WriteLine($"\nResultat: {result}. Status i databasen: {ada.Status}.");

        Assert.Equal(PickupResult.Conflict, result);
        Assert.Equal(OrderStatus.Ready, ada.Status);
        Assert.DoesNotContain(ada.StatusHistory, s => s.Status == OrderStatus.PickedUp);
        Assert.False(db.ChangeTracker.HasChanges(), "Contexten har fortsatt ulagrede endringer:\n" + db.ChangeTracker.DebugView.ShortView);
    }

    [Fact]
    public async Task Level4A_ClientWithTheCurrentVersion_PicksUp()
    {
        var version = await ReadVersionAsync(SeedData.JonasOrderId);

        await using var db = fixture.CreateContext(output.WriteLine);
        var result = await new KaffebarRepository(db).MarkAsPickedUpAsync(SeedData.JonasOrderId, version);

        Assert.Equal(PickupResult.PickedUp, result);
    }
}
