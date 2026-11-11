package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManager;
import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Kaffebonne;
import no.soprasteria.kaffebar.rapport.RapportRad;
import no.soprasteria.kaffebar.rapport.RapportRepository;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.function.Function;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Nivå 4, oppgave B (bonus): Ser rapporten en bestilling som er lagt inn i samme transaksjon?
 */
class RapportFlushTest {

    private static final LocalDate JANUAR_2027 = LocalDate.of(2027, 1, 1);

    private static EntityManagerFactory emf;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
    }

    @Test
    void nativeQuerySerNyBestillingISammeTransaksjon() {
        assertThat(rapportEtterNyBestilling(repo -> repo.toppBonnerMedNativeQuery()))
                .contains(forventetRad());
    }

    @Test
    void jdbcSerNyBestillingISammeTransaksjon() {
        assertThat(rapportEtterNyBestilling(repo -> repo.toppBonnerMedJdbc()))
                .contains(forventetRad());
    }

    private static RapportRad forventetRad() {
        return new RapportRad("Tim Wendelboe", JANUAR_2027, 1, "Gatomboya", 50, new BigDecimal("2500.00"));
    }

    private static List<RapportRad> rapportEtterNyBestilling(Function<RapportRepository, List<RapportRad>> rapport) {
        try (var em = emf.createEntityManager()) {
            em.getTransaction().begin();
            try {
                leggInnStorBestillingIJanuar(em);
                return rapport.apply(new RapportRepository(em));
            } finally {
                // Ingen commit: databasen skal være som før etter testen
                em.getTransaction().rollback();
            }
        }
    }

    private static void leggInnStorBestillingIJanuar(EntityManager em) {
        var gatomboya = em.createQuery("SELECT k FROM Kaffebonne k WHERE k.navn = 'Gatomboya'", Kaffebonne.class)
                .getSingleResult();
        var bestilling = new Bestilling("Storkunde AS", JANUAR_2027.atTime(10, 0));
        bestilling.leggTilLinje(new Bestillingslinje(gatomboya, 50, new BigDecimal("50.00")));
        em.persist(bestilling);
    }
}
