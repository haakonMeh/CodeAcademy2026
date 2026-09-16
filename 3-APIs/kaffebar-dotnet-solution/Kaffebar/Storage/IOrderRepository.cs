using Kaffebar.Models;

namespace Kaffebar.Storage;

/// <summary>
/// Lagringen, bak et lite grensesnitt.
/// </summary>
/// <remarks>
/// Grensesnittet er ikke over-engineering for et API som lagrer fire ordrer i minne.
/// Det er der av to grunner: kontrolleren skal kunne testes uten en ekte lagring, og
/// dagen noen bytter til EF Core skal det være ett sted å gjøre det.
/// </remarks>
public interface IOrderRepository
{
    IReadOnlyList<Coffee> Menu { get; }

    Coffee? FindCoffee(Guid coffeeId);

    Order? Find(Guid orderId);

    /// <summary>Nyeste først, filtrert på status og paginert. Oppgave 8.</summary>
    IReadOnlyList<Order> List(OrderStatus? status, int limit, int offset);

    Order Create(CreateOrderRequest request, Coffee coffee);

    /// <summary>
    /// Setter status. Returnerer også forrige status, slik at kalleren kan legge
    /// <c>previousStatus</c> på hendelsen — og la være å publisere når ingenting
    /// faktisk endret seg.
    /// </summary>
    (Order Order, OrderStatus PreviousStatus)? SetStatus(Guid orderId, OrderStatus status);

    /// <summary>Eldste ordre med en gitt status. Brukes av den valgfrie auto-baristaen.</summary>
    Order? OldestWithStatus(OrderStatus status);

    /// <summary>Tømmer og seeder på nytt. Brukes av testene.</summary>
    void Reset();
}
