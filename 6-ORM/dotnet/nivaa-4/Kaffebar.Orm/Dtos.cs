using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

/// <summary>Oppgave 2A: kvitteringen som en ren lesemodell, uten entiteter og uten change tracking.</summary>
public record ReceiptDto(string CustomerName, OrderStatus Status, decimal TotalPrice, List<ReceiptLineDto> Lines);

public record ReceiptLineDto(int Quantity, string DrinkName, CoffeeSize Size, string Bean, string Roastery);

/// <summary>Oppgave 2C: én linje kassa-appen vil legge inn. Bare id-en til bønnen, ikke hele bønnen.</summary>
public record NewLine(string DrinkName, CoffeeSize Size, int Quantity, decimal UnitPrice, int BeanId);

/// <summary>Oppgave 3A: det kampanje-appen skal vise. Den ordinære prisen og kampanjeprisen side om side.</summary>
public record CampaignReceipt(Guid OrderId, string CustomerName, decimal OrdinaryPrice, decimal CampaignPrice);

/// <summary>Oppgave 4A: utfallet når en barista prøver å markere en ordre som hentet.</summary>
public enum PickupResult
{
    PickedUp,
    NotReady,
    NotFound,

    /// <summary>Noen andre endret ordren mellom vi leste den og prøvde å lagre.</summary>
    Conflict
}

/// <summary>Oppgave 4B: én rad i rapporten.</summary>
public record BeanSalesRow(DateOnly Month, string Roastery, string Bean, int CupsSold, decimal Revenue, int Rank);

/// <summary>Oppgave 4C: én bestilling fra nettbutikken, slik den kommer i importfilen.</summary>
public record NewOrder(string CustomerName, DateTimeOffset CreatedAt, List<NewLine> Lines);
