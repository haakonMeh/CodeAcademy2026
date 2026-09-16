namespace Kaffebar.Models;

/// <summary>En kaffedrikk på menyen.</summary>
/// <param name="Id">Fast uuid. Brukes som <c>coffeeId</c> når man bestiller.</param>
/// <param name="Name">Navnet slik det står på menyen.</param>
/// <param name="Price">Pris i norske kroner.</param>
public record Coffee(Guid Id, string Name, decimal Price);
