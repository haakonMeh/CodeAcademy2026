using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Xunit.Abstractions;
using static Kaffebar.Orm.Tests.Print;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 3: Kampanjetabben. Oppgave 3A.</summary>
public class Level3Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    // Hver test har sin egen ordre. Lagrer den naive varianten kampanjeprisen, skal det bare
    // velte testen som ser etter nettopp det, ikke de andre.
    private const decimal AdasOrdinaryPrice = 52m;     // Kaffe Latte, medium, Tim Wendelboe
    private const decimal IngridsOrdinaryPrice = 226m; // 3 × Mocha (Fuglen) + Filterkaffe, large (Tim Wendelboe)

    private void PrintReceipt(CampaignReceipt receipt)
    {
        output.Heading("Kampanjekvitteringen");
        output.Facts(
            ("Kunde", receipt.CustomerName),
            ("Ordinær pris", Kr(receipt.OrdinaryPrice)),
            ("Kampanjepris", Kr(receipt.CampaignPrice)));
    }

    [Fact]
    public async Task Level3A_CampaignReceipt_GivesDiscountOnTimWendelboe()
    {
        await using var db = fixture.CreateContext(output.WriteLine);

        var receipt = await new KaffebarRepository(db).GenerateCampaignReceiptAsync(SeedData.AdaOrderId);

        Assert.NotNull(receipt);
        PrintReceipt(receipt);
        output.Tracked(db);

        Assert.Equal(AdasOrdinaryPrice, receipt.OrdinaryPrice);
        Assert.Equal(44.20m, receipt.CampaignPrice);
    }

    [Fact]
    public async Task Level3A_CampaignReceipt_NoDiscountWithoutTimWendelboe()
    {
        await using var db = fixture.CreateContext(output.WriteLine);

        var receipt = await new KaffebarRepository(db).GenerateCampaignReceiptAsync(SeedData.KariOrderId);

        Assert.NotNull(receipt);
        PrintReceipt(receipt);
        output.Tracked(db);

        Assert.Equal(receipt.OrdinaryPrice, receipt.CampaignPrice);
    }

    [Fact]
    public async Task Level3A_CampaignReceipt_NeverChangesTheOrderInTheDatabase()
    {
        // Én context = én request i et API. Først vises kampanjekvitteringen til Ingrid,
        // så flytter baristaen Karis ordre videre, og det lagres.
        var counter = new QueryCounter();
        await using (var db = fixture.CreateContext(output.WriteLine, counter))
        {
            var repository = new KaffebarRepository(db);

            var receipt = await repository.GenerateCampaignReceiptAsync(SeedData.IngridOrderId);
            PrintReceipt(receipt!);
            Assert.Equal(192.10m, receipt!.CampaignPrice);

            // DebugView sammenligner ikke selv. SaveChanges kaller DetectChanges før den
            // lagrer, så vi gjør det samme her for å se nøyaktig det SaveChanges vil se.
            db.ChangeTracker.DetectChanges();
            output.Heading("Change trackeren før baristaen lagrer");
            var view = db.ChangeTracker.DebugView.LongView.TrimEnd();
            output.WriteLine(view.Length == 0
                ? "  Tom. Ingen entiteter sporet."
                : string.Join("\n", view.Split('\n').Select(line => "  " + line)));

            counter.Reset();
            await repository.UpdateStatusAsync(SeedData.KariOrderId, OrderStatus.Ready);
            output.Commands(counter, "Kommandoer fra UpdateStatusAsync (Kari)");
        }

        await using var fresh = fixture.CreateContext();
        var ingrid = await fresh.Orders.FindAsync(SeedData.IngridOrderId);
        var kari = await fresh.Orders.FindAsync(SeedData.KariOrderId);
        output.Heading("I databasen etterpå");
        output.Facts(
            ("Ingrid, TotalPrice", Kr(ingrid!.TotalPrice)),
            ("Kari, Status", kari!.Status));

        Assert.Equal(IngridsOrdinaryPrice, ingrid.TotalPrice);
    }
}
