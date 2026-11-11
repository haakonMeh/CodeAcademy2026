using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 3C (bonus): Masseoppdatering.
public partial class KaffebarRepository
{
    /// <summary>Alt som har stått Ready siden før <paramref name="cutoff"/> er hentet.</summary>
    public Task<int> PickUpStaleOrdersAsync(DateTimeOffset cutoff)
    {
        // TODO 3C: skriv den først med en løkke over sporede entiteter, og så med
        //   ExecuteUpdateAsync. Sammenlign tallene testen skriver ut. Og glem ikke
        //   statushistorikken: db.Database.ExecuteSqlAsync($"INSERT ... SELECT ...") finnes.
        //   Tabellen heter status_changes (order_id, status, changed_at), og status lagres som
        //   tekst med C#-navnet: 'Ready', 'PickedUp'.
        throw new NotImplementedException("Oppgave 3C");
    }
}
