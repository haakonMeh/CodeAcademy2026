package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.tjeneste.BestillingTjeneste;
import no.soprasteria.kaffebar.tjeneste.NyLinje;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Nivå 2, oppgave C: Kassa-appen skriver til databasen.
 */
class KassaTest {

    private static EntityManagerFactory emf;
    private static BestillingTjeneste tjeneste;

    private Long registrertId;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
        tjeneste = new BestillingTjeneste(emf);
    }

    @AfterEach
    void ryddOpp() {
        // Andre tester forventer nøyaktig 100 bestillinger
        if (registrertId != null) {
            emf.runInTransaction(em -> em.remove(em.find(Bestilling.class, registrertId)));
        }
    }

    @Test
    void registrerLagrerLinjeneOgTotalprisen() {
        var bonner = kaffebonneIder();

        registrertId = tjeneste.registrer("Kari Kassatest", List.of(
                new NyLinje(bonner.get(0), 2, new BigDecimal("55.00")),
                new NyLinje(bonner.get(1), 1, new BigDecimal("60.00"))));

        try (var em = emf.createEntityManager()) {
            var lagret = em.find(Bestilling.class, registrertId);
            assertThat(lagret.getLinjer()).as("Linjer i databasen").hasSize(2);
            assertThat(lagret.getTotalpris()).as("Totalpris i databasen").isEqualByComparingTo("170.00");
        }
    }

    @Test
    void fjernLinjeSletterLinjenOgOppdatererTotalprisen() {
        Long bestillingId;
        Bestillingslinje linjeSomFjernes;
        BigDecimal totalprisFoer;
        int antallLinjerFoer;
        try (var em = emf.createEntityManager()) {
            bestillingId = em.createQuery(
                            "SELECT b.id FROM Bestilling b WHERE size(b.linjer) >= 2 ORDER BY b.id", Long.class)
                    .setFirstResult(5)
                    .setMaxResults(1)
                    .getSingleResult();
            var bestilling = em.find(Bestilling.class, bestillingId);
            linjeSomFjernes = bestilling.getLinjer().getFirst();
            totalprisFoer = bestilling.getTotalpris();
            antallLinjerFoer = bestilling.getLinjer().size();
        }

        tjeneste.fjernLinje(bestillingId, linjeSomFjernes.getId());

        try (var em = emf.createEntityManager()) {
            assertThat(em.find(Bestillingslinje.class, linjeSomFjernes.getId()))
                    .as("Linjen skal være slettet fra databasen")
                    .isNull();
            var bestilling = em.find(Bestilling.class, bestillingId);
            assertThat(bestilling.getLinjer()).hasSize(antallLinjerFoer - 1);
            assertThat(bestilling.getTotalpris())
                    .as("Totalprisen skal være redusert med linjesummen")
                    .isEqualByComparingTo(totalprisFoer.subtract(linjeSomFjernes.getLinjesum()));
        }
    }

    private static List<Long> kaffebonneIder() {
        try (var em = emf.createEntityManager()) {
            return em.createQuery("SELECT k.id FROM Kaffebonne k ORDER BY k.id", Long.class).getResultList();
        }
    }
}
