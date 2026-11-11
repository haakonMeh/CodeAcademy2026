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
        // Feilen var at rabatten ble regnet ut ved å endre en managed entitet. Ved commit sammenligner
        // Hibernate alle managed entiteter med øyeblikksbildet fra innlastingen (dirty checking) og
        // skriver endringene til databasen – uten at vi kalte persist() eller merge().
        //
        // Løsning: entiteten brukes bare til lesing, og kampanjeprisen regnes ut i en ny verdi.
        // Dette er en ren lese-operasjon, så vi trenger heller ingen transaksjon.
        //
        // Alternativer:
        //  - em.detach(bestilling) før endringen (skjørt: lett å glemme, og lazy-lasting slutter å virke)
        //  - query-hintet org.hibernate.readOnly / session.setDefaultReadOnly(true)
        //  - DTO-projeksjon i JPQL: SELECT new ...(b.id, b.kundenavn, b.totalpris) FROM Bestilling b ...
        try (var em = emf.createEntityManager()) {
            var bestilling = em.find(Bestilling.class, bestillingId);
            var ordinaerPris = bestilling.getTotalpris();
            var kampanjepris = harBonnerFraKampanjebrenneriet(bestilling)
                    ? ordinaerPris.multiply(RABATTFAKTOR).setScale(2, RoundingMode.HALF_UP)
                    : ordinaerPris;

            return new KampanjeKvittering(
                    bestilling.getId(),
                    bestilling.getKundenavn(),
                    ordinaerPris,
                    kampanjepris);
        }
    }

    private static boolean harBonnerFraKampanjebrenneriet(Bestilling bestilling) {
        return bestilling.getLinjer().stream()
                .anyMatch(linje -> KAMPANJEBRENNERI.equals(linje.getKaffebonne().getBrenneri().getNavn()));
    }
}
