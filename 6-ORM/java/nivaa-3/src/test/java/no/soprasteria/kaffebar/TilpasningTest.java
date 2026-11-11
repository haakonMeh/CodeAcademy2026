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

class TilpasningTest {

    private static final Logger LOG = LoggerFactory.getLogger(TilpasningTest.class);

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
    void kvitteringMedTilpasningerKreverMaksToSporringer() {
        try (var em = emf.createEntityManager()) {
            var bestillinger = new KaffebarRepository(em).hentBestillingerMedTilpasninger();
            var kvittering = new Kvitteringsskriver().lagKvitteringMedTilpasninger(bestillinger);

            LOG.info("Kvittering:\n{}", kvittering);
            LOG.info(QueryCountLogger.oppsummering());

            assertThat(bestillinger)
                    .as("Hver bestilling skal bare forekomme én gang")
                    .hasSize(TestData.ANTALL_BESTILLINGER)
                    .doesNotHaveDuplicates();
            assertThat(kvittering)
                    .contains("Tim Wendelboe")
                    .contains("  + Havremelk", "  + Ekstra shot");
            assertThat(QueryCountLogger.getSelectCount())
                    .as("Antall SELECT-er for å skrive kvitteringen med tilpasninger")
                    .isLessThanOrEqualTo(2);
        }
    }

    @Test
    void tilpasningerTellesRiktig() {
        try (var em = emf.createEntityManager()) {
            var bestillinger = new KaffebarRepository(em).hentBestillingerMedTilpasninger();

            long antallTilpasninger = bestillinger.stream()
                    .mapToLong(b -> b.getTilpasninger().size())
                    .sum();
            long antallLinjer = bestillinger.stream()
                    .mapToLong(b -> b.getLinjer().size())
                    .sum();

            assertThat(antallTilpasninger)
                    .as("Ingen dupliserte tilpasninger (kartesisk produkt?)")
                    .isEqualTo(em.createQuery("SELECT count(t) FROM Tilpasning t", Long.class).getSingleResult());
            assertThat(antallLinjer)
                    .as("Ingen dupliserte linjer (kartesisk produkt?)")
                    .isEqualTo(em.createQuery("SELECT count(l) FROM Bestillingslinje l", Long.class).getSingleResult());
        }
    }
}
