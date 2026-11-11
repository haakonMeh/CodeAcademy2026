using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 1A: Den naive spørringen.
public partial class KaffebarRepository
{
    /// <summary>Den naive varianten: bare ordrene, ingenting av det de peker på.</summary>
    public Task<List<Order>> GetAllOrdersAsync() =>
        db.Orders.OrderBy(o => o.CreatedAt).ToListAsync();
}
