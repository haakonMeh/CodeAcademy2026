using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

/// <summary>Oppgave 1A: skriver ut dagens kvitteringer, én linje per kopp.</summary>
public static class Receipts
{
    public static void Write(IEnumerable<Order> orders, Action<string> write)
    {
        foreach (var order in orders)
        {
            write($"{order.CustomerName} ({order.Status}) – {order.TotalPrice} kr");

            foreach (var line in order.Lines)
                write($"    {line.Quantity} × {line.DrinkName} ({line.Size}) – {line.Bean.Name} fra {line.Bean.Roastery.Name}");
        }
    }
}
