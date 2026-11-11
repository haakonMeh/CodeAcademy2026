using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 2B: To samlinger.
public partial class KaffebarRepository
{
    /// <summary>Alle bestillinger med linjer (til og med brenneri) OG statushistorikk.</summary>
    public Task<List<Order>> GetOrdersWithLinesAndHistoryAsync()
    {
        // TODO 2B: ta med både Lines og StatusHistory. Se hvor mange rader du får.
        throw new NotImplementedException("Oppgave 2B");
    }
}
