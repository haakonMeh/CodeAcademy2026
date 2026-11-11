using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 4A: Kollisjonen.
public partial class KaffebarRepository
{
    /// <summary>
    /// Kunden henter kaffen. Kan skje én gang. Står to baristaer med samme ordre, skal den
    /// ene få <see cref="PickupResult.Conflict"/>, ikke overskrive den andre.
    /// </summary>
    public async Task<PickupResult> MarkAsPickedUpAsync(Order order)
    {
        if (order.Status != OrderStatus.Ready)
            return PickupResult.NotReady;

        order.Status = OrderStatus.PickedUp;
        order.StatusHistory.Add(new StatusChange { Status = OrderStatus.PickedUp, ChangedAt = DateTimeOffset.UtcNow });

        // TODO 4A: hva skjer hvis noen andre har hentet ut kaffen siden vi leste ordren?
        await db.SaveChangesAsync();
        return PickupResult.PickedUp;
    }

    /// <summary>
    /// Det samme, men kollisjonen skjer mellom to kall: klienten så ordren med versjon
    /// <paramref name="expectedVersion"/>, og sender den med når baristaen trykker «Hentet».
    /// </summary>
    public Task<PickupResult> MarkAsPickedUpAsync(Guid orderId, uint expectedVersion)
    {
        // TODO 4A, del 2: hent ordren, sjekk versjonen og marker den som hentet.
        //   Finnes ikke ordren, er svaret NotFound.
        throw new NotImplementedException("Oppgave 4A, del 2");
    }
}
