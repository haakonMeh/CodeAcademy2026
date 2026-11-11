using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

// Oppgave 4C (bonus): Black Friday-importen. En kollega har skrevet dette, og testene er røde.
public partial class KaffebarRepository
{
    /// <summary>Importerer bestillinger fra nettbutikken, og returnerer antallet.</summary>
    public async Task<int> ImportOrdersAsync(IReadOnlyList<NewOrder> newOrders)
    {
        foreach (var item in newOrders)
        {
            db.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = item.CustomerName,
                Status = OrderStatus.Pending,
                CreatedAt = item.CreatedAt,
                TotalPrice = item.Lines.Sum(l => l.UnitPrice * l.Quantity),
                Lines = item.Lines
                    .Select(l => new OrderLine
                    {
                        DrinkName = l.DrinkName,
                        Size = l.Size,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice,
                        BeanId = l.BeanId
                    })
                    .ToList()
            });
            await db.SaveChangesAsync();
        }

        return newOrders.Count;
    }
}
