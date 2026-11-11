using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 2C: Kassa-appen skriver.
public partial class KaffebarRepository
{
    /// <summary>Registrerer en ny bestilling og returnerer id-en.</summary>
    public async Task<Guid> RegisterOrderAsync(string customerName, List<NewLine> lines)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Status = OrderStatus.Pending,
            CreatedAt = now
        };
        order.StatusHistory.Add(new StatusChange { Status = OrderStatus.Pending, ChangedAt = now });
        foreach (var line in lines)
        {
            order.AddLine(new OrderLine
            {
                DrinkName = line.DrinkName,
                Size = line.Size,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                BeanId = line.BeanId
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    /// <summary>Kunden angrer på én av linjene.</summary>
    public async Task RemoveLineAsync(Guid orderId, int lineId)
    {
        var order = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == orderId);
        order.RemoveLine(order.Lines.Single(l => l.Id == lineId));
        await db.SaveChangesAsync();
    }
}
