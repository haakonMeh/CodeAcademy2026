namespace Kaffebar.Orm.Domain;

/// <summary>Én overgang i livssyklusen, f.eks. Pending → Brewing, med tidspunkt.</summary>
public class StatusChange
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset ChangedAt { get; set; }

    public Guid OrderId { get; set; }
}
