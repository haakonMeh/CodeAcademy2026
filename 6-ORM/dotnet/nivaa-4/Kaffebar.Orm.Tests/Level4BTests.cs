using Kaffebar.Orm.Data;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 4: Den komplekse lesemodellen. Oppgave 4B.</summary>
public class Level4BTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    private void PrintReport(string title, List<BeanSalesRow> report)
    {
        output.Heading(title);
        output.Table(
            ["Måned", "Brenneri", "Rang", "Bønne", "Kopper", "Omsetning"],
            report.Select(r => new object?[] { r.Month.ToString("yyyy-MM"), r.Roastery, r.Rank, r.Bean, r.CupsSold, r.Revenue }));
    }

    private async Task<int> LinesInDatabaseAsync()
    {
        await using var db = fixture.CreateContext();
        return await db.OrderLines.CountAsync();
    }

    [Fact]
    public async Task Level4B_TopBeans_MatchesTheSameCalculationInCSharp()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var report = await new KaffebarRepository(db).GetTopBeansPerRoasteryPerMonthAsync();
        var expected = await ExpectedReportAsync();

        PrintReport("Rapporten", report);
        if (!report.SequenceEqual(expected))
            PrintReport("Den samme rapporten regnet ut i C#", expected);

        output.Heading("Resultat");
        output.Facts(
            ("Rader i rapporten", report.Count),
            ("Linjer i databasen", await LinesInDatabaseAsync()));
        output.Commands(counter);

        Assert.Equal(expected, report);
        Assert.Equal(1, counter.SelectCount);
        // Databasen skal gjøre jobben: bare rapportradene kommer over nettverket, ikke alle linjene.
        Assert.Equal(report.Count, counter.RowsRead);
    }

    [Fact]
    public async Task Level4B_Bonus_AdoNet_GivesTheSameReport()
    {
        var counter = new QueryCounter();
        var logged = 0;
        await using var db = fixture.CreateContext(s => { logged++; output.WriteLine(s); }, counter);

        var report = await new KaffebarRepository(db).GetTopBeansWithAdoNetAsync();

        output.Heading("Resultat");
        output.Facts(
            ("Rader i rapporten", report.Count),
            ("Meldinger i SQL-loggen til EF", logged));
        output.Commands(counter);

        Assert.Equal(await ExpectedReportAsync(), report);
    }

    /// <summary>
    /// Fasiten, regnet ut på den tunge måten: alle linjene inn i minnet, og så LINQ to
    /// Objects. Det er dette rapporten din skal slippe å gjøre.
    /// </summary>
    private async Task<List<BeanSalesRow>> ExpectedReportAsync()
    {
        await using var db = fixture.CreateContext();
        var lines = await db.OrderLines
            .AsNoTracking()
            .Select(l => new { l.Order.CreatedAt, Roastery = l.Bean.Roastery.Name, Bean = l.Bean.Name, l.Quantity, l.UnitPrice })
            .ToListAsync();

        return lines
            .GroupBy(l => (Month: new DateOnly(l.CreatedAt.UtcDateTime.Year, l.CreatedAt.UtcDateTime.Month, 1), l.Roastery, l.Bean))
            .Select(g => (g.Key, Cups: g.Sum(l => l.Quantity), Revenue: g.Sum(l => l.Quantity * l.UnitPrice)))
            .GroupBy(x => (x.Key.Month, x.Key.Roastery))
            .SelectMany(group => group
                .OrderByDescending(x => x.Cups)
                .ThenByDescending(x => x.Revenue)
                .ThenBy(x => x.Key.Bean, StringComparer.Ordinal)
                .Take(3)
                .Select((x, index) => new BeanSalesRow(x.Key.Month, x.Key.Roastery, x.Key.Bean, x.Cups, x.Revenue, index + 1)))
            .OrderBy(r => r.Roastery, StringComparer.Ordinal)
            .ThenBy(r => r.Month)
            .ThenBy(r => r.Rank)
            .ToList();
    }
}
