using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 2A: Include og projeksjon.
public partial class KaffebarRepository
{
    /// <summary>Hele grafen i én spørring. EF sin <c>JOIN FETCH</c>.</summary>
    public Task<List<Order>> GetAllOrdersWithDetailsAsync() =>
        db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.Bean)
                    .ThenInclude(b => b.Roastery)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

    /// <summary>
    /// Alternativet: be om akkurat de kolonnene kvitteringen trenger. Ingen entiteter, ingen
    /// tracking, og ingen <c>Include</c>. EF ser selv hvilke tabeller den må joine.
    /// </summary>
    public Task<List<ReceiptDto>> GetReceiptsAsync() =>
        db.Orders
            .OrderBy(o => o.CreatedAt)
            .Select(o => new ReceiptDto(
                o.CustomerName,
                o.Status,
                o.TotalPrice,
                o.Lines.Select(l => new ReceiptLineDto(
                    l.Quantity, l.DrinkName, l.Size, l.Bean.Name, l.Bean.Roastery.Name)).ToList()))
            .ToListAsync();
}
