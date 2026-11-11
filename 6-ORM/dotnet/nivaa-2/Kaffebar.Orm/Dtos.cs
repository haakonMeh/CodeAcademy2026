using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

/// <summary>Oppgave 2A: kvitteringen som en ren lesemodell, uten entiteter og uten change tracking.</summary>
public record ReceiptDto(string CustomerName, OrderStatus Status, decimal TotalPrice, List<ReceiptLineDto> Lines);

public record ReceiptLineDto(int Quantity, string DrinkName, CoffeeSize Size, string Bean, string Roastery);

/// <summary>Oppgave 2C: én linje kassa-appen vil legge inn. Bare id-en til bønnen, ikke hele bønnen.</summary>
public record NewLine(string DrinkName, CoffeeSize Size, int Quantity, decimal UnitPrice, int BeanId);
