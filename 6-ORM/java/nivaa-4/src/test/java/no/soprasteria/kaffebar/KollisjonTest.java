package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import jakarta.persistence.OptimisticLockException;
import jakarta.persistence.RollbackException;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class KollisjonTest {

    private static EntityManagerFactory emf;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
    }

    @Test
    void toBaristaerEndrerSammeBestillingSamtidig() {
        var id = Testbestillinger.id(emf, Testbestillinger.KOLLISJON);

        try (var barista1 = emf.createEntityManager();
             var barista2 = emf.createEntityManager()) {

            // Begge baristaene henter bestillingen før noen av dem har lagret
            barista1.getTransaction().begin();
            var bestillingHos1 = barista1.find(Bestilling.class, id);

            barista2.getTransaction().begin();
            var bestillingHos2 = barista2.find(Bestilling.class, id);

            // Barista 1 markerer den som klar og lagrer først
            bestillingHos1.setStatus(Bestilling.Status.KLAR);
            barista1.getTransaction().commit();

            // Barista 2 har en utdatert utgave og markerer den som hentet
            bestillingHos2.setStatus(Bestilling.Status.HENTET);

            assertThatThrownBy(() -> barista2.getTransaction().commit())
                    .as("Commit fra barista 2 skal feile, fordi bestillingen er endret siden den ble hentet")
                    .isInstanceOf(RollbackException.class)
                    .hasCauseInstanceOf(OptimisticLockException.class);
        }

        try (var em = emf.createEntityManager()) {
            assertThat(em.find(Bestilling.class, id).getStatus())
                    .as("Endringen fra barista 1 skal stå, ikke overskrives i stillhet")
                    .isEqualTo(Bestilling.Status.KLAR);
        }
    }
}
