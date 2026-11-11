package no.soprasteria.kaffebar.tjeneste;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;

import java.math.BigDecimal;
import java.math.RoundingMode;

/**
 * Kampanje: 15 % rabatt på bestillinger med bønner fra Tim Wendelboe.
 * Rabatten skal kun vises i kampanje-appen – ordren skal ikke endres i databasen.
 */
public class KampanjeTjeneste {

    public static final String KAMPANJEBRENNERI = "Tim Wendelboe";
    public static final BigDecimal RABATTFAKTOR = new BigDecimal("0.85");

    private final EntityManagerFactory emf;

    public KampanjeTjeneste(EntityManagerFactory emf) {
        this.emf = emf;
    }

    public KampanjeKvittering genererKampanjeKvittering(Long bestillingId) {
        try (var em = emf.createEntityManager()) {
            em.getTransaction().begin();

            var bestilling = em.find(Bestilling.class, bestillingId);
            var ordinaerPris = bestilling.getTotalpris();

            if (harBonnerFraKampanjebrenneriet(bestilling)) {
                bestilling.setTotalpris(ordinaerPris.multiply(RABATTFAKTOR).setScale(2, RoundingMode.HALF_UP));
            }

            em.getTransaction().commit();

            return new KampanjeKvittering(
                    bestilling.getId(),
                    bestilling.getKundenavn(),
                    ordinaerPris,
                    bestilling.getTotalpris());
        }
    }

    private static boolean harBonnerFraKampanjebrenneriet(Bestilling bestilling) {
        return bestilling.getLinjer().stream()
                .anyMatch(linje -> KAMPANJEBRENNERI.equals(linje.getKaffebonne().getBrenneri().getNavn()));
    }
}
