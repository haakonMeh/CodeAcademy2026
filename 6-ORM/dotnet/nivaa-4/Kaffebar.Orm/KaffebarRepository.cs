using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

/// <summary>
/// All databasetilgang i kaffebaren. Én instans lever like lenge som sin
/// <see cref="KaffebarDbContext"/>, slik den ville gjort per request i ASP.NET Core.
/// </summary>
/// <remarks>
/// Klassen er delt i én fil per oppgave (<c>KaffebarRepository.Level1A.cs</c>,
/// <c>…Level2A.cs</c> osv.), slik at du kan kopiere inn fasiten for én oppgave uten å
/// røre de andre. Her ligger bare det som er ferdig skrevet fra starten.
/// </remarks>
public partial class KaffebarRepository(KaffebarDbContext db)
{
    /// <summary>Baristaen flytter en ordre ett steg videre. Ferdig skrevet, men se oppgave 3A.</summary>
    public async Task UpdateStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await db.Orders.SingleAsync(o => o.Id == orderId);
        order.Status = status;
        order.StatusHistory.Add(new StatusChange { Status = status, ChangedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    /// <summary>Henter en ordre baristaen skal jobbe med. Den blir sporet av denne contexten.</summary>
    public Task<Order?> GetOrderAsync(Guid orderId) =>
        db.Orders.SingleOrDefaultAsync(o => o.Id == orderId);
}
