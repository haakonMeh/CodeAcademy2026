using System.Text.Json;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 3B: Entiteter ut av datalaget.
public partial class KaffebarRepository
{
    /// <summary>
    /// Bestillingen som JSON til kassa-appen, eller <c>null</c> hvis den ikke finnes.
    /// Kassa-appen forventer dette formatet:
    /// <code>
    /// {
    ///   "id": "0de50001-0000-4000-8000-000000000004",
    ///   "customerName": "Ingrid",
    ///   "status": "Pending",
    ///   "totalPrice": 226.00,
    ///   "lines": [
    ///     { "bean": "Yirgacheffe", "roastery": "Fuglen", "quantity": 3, "unitPrice": 59.00 }
    ///   ],
    ///   "statusHistory": [ "Pending" ]
    /// }
    /// </code>
    /// </summary>
    public async Task<string?> GetOrderJsonAsync(Guid orderId)
    {
        var order = await db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => new OrderJson(
                o.Id,
                o.CustomerName,
                o.Status.ToString(),
                o.TotalPrice,
                o.Lines
                    .OrderBy(l => l.Id)
                    .Select(l => new OrderLineJson(l.Bean.Name, l.Bean.Roastery.Name, l.Quantity, l.UnitPrice))
                    .ToList(),
                o.StatusHistory
                    .OrderBy(s => s.ChangedAt)
                    .Select(s => s.Status.ToString())
                    .ToList()))
            .SingleOrDefaultAsync();

        return order is null ? null : JsonSerializer.Serialize(order, JsonSerializerOptions.Web);
    }

    private record OrderJson(
        Guid Id,
        string CustomerName,
        string Status,
        decimal TotalPrice,
        List<OrderLineJson> Lines,
        List<string> StatusHistory);

    private record OrderLineJson(string Bean, string Roastery, int Quantity, decimal UnitPrice);
}
