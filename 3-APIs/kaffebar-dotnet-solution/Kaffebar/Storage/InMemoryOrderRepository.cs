using System.Collections.Concurrent;
using Kaffebar.Models;

namespace Kaffebar.Storage;

/// <summary>
/// Lagring i minne.
/// </summary>
/// <remarks>
/// Bevisst valg: ingen database, ingen EF Core, ingen migrasjoner. API-et starter rent
/// hver gang, og ingen bruker workshoptid på en migrasjon som feiler eller en container
/// som ikke kommer opp. Datasettet er lite nok til å ligge i en ordbok, og at alt
/// nullstilles ved omstart er en fordel — du kommer alltid tilbake til et kjent
/// utgangspunkt.
///
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> og ikke <c>Dictionary</c>: Kestrel
/// håndterer requests parallelt, så dette er delt, foranderlig tilstand. Det er den
/// eneste trådsikkerhets-detaljen i hele prosjektet, og den er verdt å peke på.
/// </remarks>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public InMemoryOrderRepository() => Reset();

    public IReadOnlyList<Coffee> Menu => Seed.Menu;

    public Coffee? FindCoffee(Guid coffeeId) =>
        Seed.Menu.FirstOrDefault(coffee => coffee.Id == coffeeId);

    public Order? Find(Guid orderId) =>
        _orders.TryGetValue(orderId, out var order) ? order : null;

    public IReadOnlyList<Order> List(OrderStatus? status, int limit, int offset) =>
        _orders.Values
            .Where(order => status is null || order.Status == status)
            .OrderByDescending(order => order.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToList();

    public Order Create(CreateOrderRequest request, Coffee coffee)
    {
        var now = Now();
        var order = new Order(
            OrderId: Guid.NewGuid(),
            CoffeeId: coffee.Id,
            CoffeeName: coffee.Name,
            CustomerName: request.CustomerName.Trim(),
            Size: request.Size,
            Quantity: request.Quantity,
            Status: OrderStatus.Pending,
            CreatedAt: now,
            UpdatedAt: now,
            MilkType: request.MilkType,
            ExtraShot: request.ExtraShot);

        _orders[order.OrderId] = order;
        return order;
    }

    public (Order Order, OrderStatus PreviousStatus)? SetStatus(Guid orderId, OrderStatus status)
    {
        if (!_orders.TryGetValue(orderId, out var existing))
        {
            return null;
        }

        // `record` er uforanderlig — `with` lager en kopi med én endret egenskap.
        var updated = existing with { Status = status, UpdatedAt = Now() };
        _orders[orderId] = updated;
        return (updated, existing.Status);
    }

    public Order? OldestWithStatus(OrderStatus status) =>
        _orders.Values
            .Where(order => order.Status == status)
            .OrderBy(order => order.CreatedAt)
            .FirstOrDefault();

    public void Reset()
    {
        _orders.Clear();
        foreach (var order in Seed.Orders(Now()))
        {
            _orders[order.OrderId] = order;
        }
    }

    /// <summary>
    /// UTC med millisekundpresisjon. .NET har 100-nanosekunders «ticks», og
    /// <c>2026-09-13T09:41:12.0045210+00:00</c> ser rart ut ved siden av
    /// TypeScript-fasitens <c>2026-09-13T09:41:12.004Z</c>. Begge er gyldig ISO-8601,
    /// men like er bedre enn nesten like.
    /// </summary>
    private static DateTimeOffset Now()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), TimeSpan.Zero);
    }
}
