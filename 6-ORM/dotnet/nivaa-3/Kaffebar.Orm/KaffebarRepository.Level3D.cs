using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

// Oppgave 3D (bonus): Den motsatte fellen. En kollega har skrevet dette, og testene er røde.
public partial class KaffebarRepository
{
    /// <summary>Lagrer en ordre kassa-appen hentet tidligere og nå sender inn igjen med ny status.</summary>
    public async Task SaveOrderAsync(Order order)
    {
        // EF oppdager endringer automatisk, så her trengs ikke noe mer.
        await db.SaveChangesAsync();
    }
}
