package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.db.QueryCountLogger;
import no.soprasteria.kaffebar.db.TestData;
import no.soprasteria.kaffebar.kvittering.Kvitteringsskriver;
import no.soprasteria.kaffebar.repository.KaffebarRepository;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import static org.assertj.core.api.Assertions.assertThat;

class KvitteringTest {

    private static final Logger LOG = LoggerFactory.getLogger(KvitteringTest.class);

    private static EntityManagerFactory emf;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
    }

    @BeforeEach
    void nullstillTeller() {
        QueryCountLogger.reset();
    }

    @Test
    void skriverKvitteringForAlleBestillinger() {
        try (var em = emf.createEntityManager()) {
            var bestillinger = new KaffebarRepository(em).hentAlleBestillinger();
            var kvittering = new Kvitteringsskriver().lagKvittering(bestillinger);

            LOG.info("Kvittering:\n{}", kvittering);
            LOG.info(QueryCountLogger.oppsummering());

            assertThat(bestillinger).hasSize(TestData.ANTALL_BESTILLINGER);
            assertThat(kvittering)
                    .contains("Kari Nordmann")
                    .contains("Tim Wendelboe", "Supreme Roastworks", "Kaffa", "Fuglen", "Jacu Coffee Roastery");
        }
    }

    @Test
    void kvitteringKreverBareEnSporring() {
        try (var em = emf.createEntityManager()) {
            var bestillinger = new KaffebarRepository(em).hentAlleBestillinger();
            new Kvitteringsskriver().lagKvittering(bestillinger);

            assertThat(bestillinger)
                    .as("Hver bestilling skal bare forekomme én gang")
                    .hasSize(TestData.ANTALL_BESTILLINGER);
            assertThat(QueryCountLogger.getSelectCount())
                    .as("Antall SELECT-er for å skrive kvitteringen")
                    .isEqualTo(1);
        }
    }
}
