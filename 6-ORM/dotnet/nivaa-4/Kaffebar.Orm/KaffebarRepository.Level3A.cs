using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm;

// Oppgave 3A: Kampanjetabben.
public partial class KaffebarRepository
{
    /// <summary>
    /// Kampanjekvittering til visning. Leser, regner og returnerer en DTO. Den endrer
    /// ingenting, og den etterlater ingenting i change trackeren som en senere
    /// <c>SaveChanges</c> kan finne på å lagre.
    /// </summary>
    public async Task<CampaignReceipt?> GenerateCampaignReceiptAsync(Guid orderId)
    {
        var order = await db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => new
            {
                o.Id,
                o.CustomerName,
                o.TotalPrice,
                Eligible = o.Lines.Any(l => l.Bean.Roastery.Name == Campaign.RoasteryName)
            })
            .SingleOrDefaultAsync();

        if (order is null)
            return null;

        return new CampaignReceipt(
            order.Id,
            order.CustomerName,
            order.TotalPrice,
            Campaign.Apply(order.TotalPrice, order.Eligible));
    }
}
