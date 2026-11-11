namespace Kaffebar.Orm.Domain;

/// <summary>En kaffebønne fra ett bestemt brenneri.</summary>
public class Bean
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Origin { get; set; } = null!;
    public string Process { get; set; } = null!;

    public int RoasteryId { get; set; }
    public virtual Roastery Roastery { get; set; } = null!;
}
