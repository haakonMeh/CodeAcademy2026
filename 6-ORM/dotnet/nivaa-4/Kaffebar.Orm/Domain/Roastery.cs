namespace Kaffebar.Orm.Domain;

/// <summary>Brenneriet som har brent bønnene.</summary>
public class Roastery
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;

    public virtual List<Bean> Beans { get; set; } = [];
}
