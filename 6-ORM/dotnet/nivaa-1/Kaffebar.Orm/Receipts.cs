using Kaffebar.Orm.Domain;

namespace Kaffebar.Orm;

/// <summary>Oppgave 1A: skriver ut dagens kvitteringer, én linje per kopp.</summary>
public static class Receipts
{
    public static void Write(IEnumerable<Order> orders, Action<string> write)
    {
        // TODO 1A: for hver ordre, skriv kundenavn, status og totalpris.
        // For hver linje i ordren, skriv antall, drikk, størrelse, bønnens navn
        // og brenneriets navn: order.Lines → line.Bean → bean.Roastery.Name.
        // Bruk write(...), ikke Console.WriteLine. Da havner det i testresultatet.
        throw new NotImplementedException("Oppgave 1A: skriv ut kvitteringene");
    }
}
