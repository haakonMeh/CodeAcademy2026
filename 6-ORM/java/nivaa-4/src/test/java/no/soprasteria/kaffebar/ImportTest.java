package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.db.QueryCountLogger;
import no.soprasteria.kaffebar.importering.ImportTjeneste;
import no.soprasteria.kaffebar.importering.NyBestilling;
import no.soprasteria.kaffebar.tjeneste.NyLinje;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.IntStream;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Nivå 4, oppgave C (bonus): Black Friday-importen.
 */
class ImportTest {

    private static final Logger LOG = LoggerFactory.getLogger(ImportTest.class);

    private static final int ANTALL = 1000;
    private static final String KUNDEPREFIKS = "Nettkunde ";

    private static EntityManagerFactory emf;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
    }

    @AfterEach
    void ryddOpp() {
        // Andre tester forventer nøyaktig 100 bestillinger
        emf.runInTransaction(em -> {
            em.createQuery("""
                            DELETE FROM Bestillingslinje l
                            WHERE l.bestilling.id IN (SELECT b.id FROM Bestilling b WHERE b.kundenavn LIKE :prefiks)""")
                    .setParameter("prefiks", KUNDEPREFIKS + "%")
                    .executeUpdate();
            em.createQuery("DELETE FROM Bestilling b WHERE b.kundenavn LIKE :prefiks")
                    .setParameter("prefiks", KUNDEPREFIKS + "%")
                    .executeUpdate();
        });
    }

    @Test
    void importererTusenBestillingerMedFaaInsertSetninger() {
        var nyeBestillinger = lagNyeBestillinger();

        QueryCountLogger.reset();
        long start = System.nanoTime();
        int importert = new ImportTjeneste(emf).importer(nyeBestillinger);
        long millis = (System.nanoTime() - start) / 1_000_000;

        LOG.info("Importerte {} bestillinger med {} linjer på {} ms", importert, ANTALL * 2, millis);
        LOG.info(QueryCountLogger.oppsummering());

        assertThat(importert).isEqualTo(ANTALL);
        try (var em = emf.createEntityManager()) {
            assertThat(em.createQuery("SELECT count(b) FROM Bestilling b WHERE b.kundenavn LIKE :prefiks", Long.class)
                    .setParameter("prefiks", KUNDEPREFIKS + "%")
                    .getSingleResult())
                    .as("Bestillinger i databasen etter import")
                    .isEqualTo(ANTALL);
        }
        assertThat(QueryCountLogger.getInsertCount())
                .as("INSERT-setninger Hibernate forbereder for %d rader", ANTALL * 3)
                .isLessThanOrEqualTo(100);
    }

    private static List<NyBestilling> lagNyeBestillinger() {
        List<Long> bonner;
        try (var em = emf.createEntityManager()) {
            bonner = em.createQuery("SELECT k.id FROM Kaffebonne k ORDER BY k.id", Long.class).getResultList();
        }
        var tidspunkt = LocalDateTime.of(2026, 11, 27, 0, 0);
        return IntStream.range(0, ANTALL)
                .mapToObj(i -> new NyBestilling(KUNDEPREFIKS + i, tidspunkt.plusSeconds(i * 37L), List.of(
                        new NyLinje(bonner.get(i % bonner.size()), 1, new BigDecimal("55.00")),
                        new NyLinje(bonner.get((i + 7) % bonner.size()), 2, new BigDecimal("50.00")))))
                .toList();
    }
}
