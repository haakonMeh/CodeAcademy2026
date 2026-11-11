using System.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 3C (bonus): Masseoppdatering.
public partial class KaffebarRepository
{
    /// <summary>
    /// Rydder baristakøen: alt som har stått Ready siden før <paramref name="cutoff"/> er hentet.
    /// Én UPDATE i databasen, ingen entiteter i minnet.
    /// </summary>
    public async Task<int> PickUpStaleOrdersAsync(DateTimeOffset cutoff)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        var stale = db.Orders.Where(o => o.Status == OrderStatus.Ready && o.CreatedAt < cutoff);
        var now = DateTimeOffset.UtcNow;

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO status_changes (order_id, status, changed_at)
            SELECT id, 'PickedUp', {now} FROM orders
            WHERE status = 'Ready' AND created_at < {cutoff}
            """);

        var updated = await stale.ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.PickedUp));

        await transaction.CommitAsync();
        return updated;
    }
}
