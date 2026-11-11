using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 1A: Den naive spørringen.
public partial class KaffebarRepository
{
    /// <summary>Alle bestillinger, eldste først.</summary>
    public Task<List<Order>> GetAllOrdersAsync()
    {
        // TODO 1A: hent alle ordrene, sortert på CreatedAt. Ikke noe mer enn det.
        throw new NotImplementedException("Oppgave 1A");
    }
}
