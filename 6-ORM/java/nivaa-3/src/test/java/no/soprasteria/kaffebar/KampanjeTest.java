package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.tjeneste.KampanjeTjeneste;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class KampanjeTest {

    private static EntityManagerFactory emf;
    private static KampanjeTjeneste kampanjeTjeneste;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
        kampanjeTjeneste = new KampanjeTjeneste(emf);
    }

    @Test
    void kampanjenEndrerIkkeOrdrenIDatabasen() {
        var bestillingId = bestillingerMedKampanjebrenneri().get(0);
        var prisFoer = totalprisIDatabasen(bestillingId);

        kampanjeTjeneste.genererKampanjeKvittering(bestillingId);

        assertThat(totalprisIDatabasen(bestillingId))
                .as("Totalprisen i databasen skal være uendret etter at kampanjekvitteringen er laget")
                .isEqualByComparingTo(prisFoer);
    }

    @Test
    void kampanjekvitteringGir15ProsentRabatt() {
        var bestillingId = bestillingerMedKampanjebrenneri().get(1);
        var ordinaerPris = totalprisIDatabasen(bestillingId);

        var kvittering = kampanjeTjeneste.genererKampanjeKvittering(bestillingId);

        assertThat(kvittering.bestillingId()).isEqualTo(bestillingId);
        assertThat(kvittering.ordinaerPris()).isEqualByComparingTo(ordinaerPris);
        assertThat(kvittering.kampanjepris())
                .isEqualByComparingTo(ordinaerPris.multiply(new BigDecimal("0.85")).setScale(2, RoundingMode.HALF_UP));
    }

    @Test
    void bestillingUtenKampanjebrenneriFaarIkkeRabatt() {
        var bestillingId = bestillingerUtenKampanjebrenneri().get(0);
        var ordinaerPris = totalprisIDatabasen(bestillingId);

        var kvittering = kampanjeTjeneste.genererKampanjeKvittering(bestillingId);

        assertThat(kvittering.kampanjepris()).isEqualByComparingTo(ordinaerPris);
    }

    private static BigDecimal totalprisIDatabasen(Long bestillingId) {
        // Ny EntityManager hver gang, så vi leser fra databasen og ikke fra en persistence context
        try (var em = emf.createEntityManager()) {
            return em.find(Bestilling.class, bestillingId).getTotalpris();
        }
    }

    private static List<Long> bestillingerMedKampanjebrenneri() {
        try (var em = emf.createEntityManager()) {
            return em.createQuery("""
                            SELECT DISTINCT l.bestilling.id FROM Bestillingslinje l
                            WHERE l.kaffebonne.brenneri.navn = :brenneri
                            ORDER BY l.bestilling.id""", Long.class)
                    .setParameter("brenneri", KampanjeTjeneste.KAMPANJEBRENNERI)
                    .getResultList();
        }
    }

    private static List<Long> bestillingerUtenKampanjebrenneri() {
        try (var em = emf.createEntityManager()) {
            return em.createQuery("""
                            SELECT b.id FROM Bestilling b
                            WHERE NOT EXISTS (
                                SELECT 1 FROM Bestillingslinje l
                                WHERE l.bestilling = b AND l.kaffebonne.brenneri.navn = :brenneri)
                            ORDER BY b.id""", Long.class)
                    .setParameter("brenneri", KampanjeTjeneste.KAMPANJEBRENNERI)
                    .getResultList();
        }
    }
}
