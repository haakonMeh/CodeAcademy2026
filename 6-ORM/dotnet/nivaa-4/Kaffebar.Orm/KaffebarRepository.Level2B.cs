using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 2B: To samlinger.
public partial class KaffebarRepository
{
    /// <summary>
    /// To søskensamlinger. Uten <c>AsSplitQuery</c> blir det én spørring med
    /// linjer × historikk rader per ordre: det kartesiske produktet.
    /// </summary>
    public Task<List<Order>> GetOrdersWithLinesAndHistoryAsync() =>
        db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.Bean)
                    .ThenInclude(b => b.Roastery)
            .Include(o => o.StatusHistory)
            .OrderBy(o => o.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();
}
