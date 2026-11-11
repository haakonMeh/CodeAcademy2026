using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 4B: Den komplekse lesemodellen.
public partial class KaffebarRepository
{
    /// <summary>Topp 3 bønner per brenneri per måned, målt i antall kopper, med omsetning.</summary>
    public Task<List<BeanSalesRow>> GetTopBeansPerRoasteryPerMonthAsync()
    {
        // TODO 4B: rå SQL med db.Database.SqlQuery<BeanSalesRow>($"...").
        //   Tabellene heter orders, order_lines, beans og roasteries (snake_case).
        //   Kolonnene i resultatet må hete det samme som egenskapene i BeanSalesRow,
        //   i snake_case: month, roastery, bean, cups_sold, revenue, rank.
        //   Rangering innen hver måned og hvert brenneri: flest kopper, så høyest omsetning,
        //   så bønnenavn. Sorter resultatet på brenneri, måned og rang.
        //   Måneden må være en date (starten av måneden i UTC) for å bli en DateOnly.
        throw new NotImplementedException("Oppgave 4B");
    }

    /// <summary>Bonus: den samme rapporten, kjørt rett på ADO.NET-tilkoblingen.</summary>
    public Task<List<BeanSalesRow>> GetTopBeansWithAdoNetAsync()
    {
        // TODO 4B, bonus: samme SQL via db.Database.GetDbConnection(). Åpne tilkoblingen,
        //   lag en DbCommand, og map hver rad til BeanSalesRow selv.
        throw new NotImplementedException("Oppgave 4B, bonus");
    }
}
