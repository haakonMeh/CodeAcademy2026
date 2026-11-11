using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

// Oppgave 3D (bonus): Den motsatte fellen.
public partial class KaffebarRepository
{
    /// <summary>Lagrer en ordre kassa-appen hentet tidligere og nå sender inn igjen med ny status.</summary>
    public Task SaveOrderAsync(Order order) => UpdateStatusAsync(order.Id, order.Status);
}
