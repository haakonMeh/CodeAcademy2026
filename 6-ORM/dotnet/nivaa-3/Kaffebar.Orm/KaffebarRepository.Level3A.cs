using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 3A: Kampanjetabben.
public partial class KaffebarRepository
{
    /// <summary>Kampanjekvittering til visning i kampanje-appen. Skal ikke endre ordren.</summary>
    public Task<CampaignReceipt?> GenerateCampaignReceiptAsync(Guid orderId)
    {
        // TODO 3A: skriv den rett fram først:
        //   1. Hent ordren med linjer, bønner og brenneri.
        //   2. Kjøpte kunden bønner fra Campaign.RoasteryName? Sett order.TotalPrice til
        //      kampanjeprisen (15 % rabatt, avrundet til to desimaler).
        //   3. Returner en CampaignReceipt med ordinær pris og kampanjepris.
        // Kjør testene for oppgave 3A. Så begynner den egentlige oppgaven.
        throw new NotImplementedException("Oppgave 3A");
    }
}
