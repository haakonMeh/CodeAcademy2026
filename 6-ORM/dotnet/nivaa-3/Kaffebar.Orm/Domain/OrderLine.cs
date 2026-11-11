namespace Kaffebar.Orm.Domain;

/// <summary>
/// Én drikk i en bestilling. I samling 3 hadde en ordre bare én kaffe. Nå kan den ha
/// flere linjer, og hver linje vet hvilken bønne baristaen brukte.
/// </summary>
public class OrderLine
{
    public int Id { get; set; }

    /// <summary>Navnet fra menyen i samling 3: «Espresso», «Cortado», «Flat White» …</summary>
    public string DrinkName { get; set; } = null!;

    public CoffeeSize Size { get; set; }
    public MilkType? MilkType { get; set; }
    public bool ExtraShot { get; set; }
    public int Quantity { get; set; }

    /// <summary>Prisen per kopp da bestillingen ble lagt inn, ikke dagens menypris.</summary>
    public decimal UnitPrice { get; set; }

    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;

    public int BeanId { get; set; }
    public virtual Bean Bean { get; set; } = null!;
}
