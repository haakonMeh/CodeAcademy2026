using Kaffebar.Orm.Data;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>Nivå 4: Den komplekse lesemodellen. Oppgave 4B.</summary>
public class Level4BTests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Level4B_TopBeans_MatchesTheSameCalculationInCSharp()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var report = await new KaffebarRepository(db).GetTopBeansPerRoasteryPerMonthAsync();

        foreach (var row in report)
            output.WriteLine($"{row.Month:yyyy-MM}  {row.Roastery,-18} #{row.Rank} {row.Bean,-15} {row.CupsSold,4} kopper {row.Revenue,9} kr");

        Assert.Equal(await ExpectedReportAsync(), report);
        Assert.Equal(1, counter.SelectCount);
        // Databasen skal gjøre jobben: bare rapportradene kommer over nettverket, ikke alle linjene.
        Assert.Equal(report.Count, counter.RowsRead);
    }

    [Fact]
    public async Task Level4B_Bonus_AdoNet_GivesTheSameReport()
    {
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(output.WriteLine, counter);

        var report = await new KaffebarRepository(db).GetTopBeansWithAdoNetAsync();

        output.WriteLine($"{report.Count} rader. QueryCounter så {counter.SelectCount} spørring(er), og loggen over er tom. Hvorfor?");
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
