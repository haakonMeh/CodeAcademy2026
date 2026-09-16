using Kaffebar.Models;
using Kaffebar.Storage;

namespace Kaffebar.Events;

/// <summary>
/// VALGFRITT — hører sammen med <see cref="OrderEventPublisher"/>.
/// </summary>
/// <remarks>
/// Flytter den eldste BREWING-ordren til READY, og deretter den eldste PENDING-ordren
/// til BREWING. Uten den skjer det ingenting i frontenden med mindre noen klikker, og
/// hele poenget med samling 4 — å se en ordre bevege seg i to vinduer samtidig —
/// faller bort.
///
/// Registreres bare når <c>Kaffebar:Barista:AutoEnabled</c> er true.
/// </remarks>
public sealed class AutoBarista(
    IOrderRepository repository,
    OrderEventPublisher events,
    BaristaOptions options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.IntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            Advance(OrderStatus.Brewing, OrderStatus.Ready);
            Advance(OrderStatus.Pending, OrderStatus.Brewing);
        }
    }

    private void Advance(OrderStatus from, OrderStatus to)
    {
        var oldest = repository.OldestWithStatus(from);
        if (oldest is null)
        {
            return;
        }

        var change = repository.SetStatus(oldest.OrderId, to);
        if (change is not null)
        {
            events.OrderStatusChanged(change.Value.Order, change.Value.PreviousStatus);
        }
    }
}
