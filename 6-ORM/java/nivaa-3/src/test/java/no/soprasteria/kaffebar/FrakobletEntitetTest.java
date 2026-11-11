package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.tjeneste.BestillingTjeneste;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Nivå 3, oppgave C (bonus): Den motsatte fellen.
 */
class FrakobletEntitetTest {

    private static EntityManagerFactory emf;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
    }

    @Test
    void endringPaaFrakobletBestillingBlirLagret() {
        // Kassa-appen henter en bestilling …
        Bestilling bestilling;
        try (var em = emf.createEntityManager()) {
            bestilling = em.createQuery("SELECT b FROM Bestilling b ORDER BY b.id DESC", Bestilling.class)
                    .setMaxResults(1)
                    .getSingleResult();
        }

        // … baristaen markerer den som klar, og kassa-appen sender den inn igjen
        bestilling.setStatus(Bestilling.Status.KLAR);
        new BestillingTjeneste(emf).lagre(bestilling);

        try (var em = emf.createEntityManager()) {
            assertThat(em.find(Bestilling.class, bestilling.getId()).getStatus())
                    .as("Status i databasen etter lagre()")
                    .isEqualTo(Bestilling.Status.KLAR);
        }
    }
}
