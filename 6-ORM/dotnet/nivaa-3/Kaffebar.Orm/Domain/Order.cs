namespace Kaffebar.Orm.Domain;

/// <summary>En bestilling i kaffebaren.</summary>
/// <remarks>
/// Navigasjonene er <c>virtual</c>. Det har ingen betydning før oppgave 1B, der vi skrur på
/// lazy loading. Da lager EF en proxy-subklasse som overstyrer dem, og det kan den bare
/// gjøre med virtuelle egenskaper.
/// </remarks>
public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Summen av linjene, lagret på ordren slik den sto på kvitteringen.</summary>
    public decimal TotalPrice { get; set; }

    public virtual List<OrderLine> Lines { get; set; } = [];
    public virtual List<StatusChange> StatusHistory { get; set; } = [];

    /// <summary>Oppgave 2C: legger til en linje og holder totalprisen riktig.</summary>
    public void AddLine(OrderLine line)
    {
        Lines.Add(line);
        TotalPrice += line.UnitPrice * line.Quantity;
    }

    /// <summary>Oppgave 2C: fjerner en linje og holder totalprisen riktig.</summary>
    public void RemoveLine(OrderLine line)
    {
        if (Lines.Remove(line))
            TotalPrice -= line.UnitPrice * line.Quantity;
    }
}
