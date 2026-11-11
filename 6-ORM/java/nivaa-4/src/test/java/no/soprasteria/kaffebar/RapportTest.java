package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.db.QueryCountLogger;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.rapport.RapportRad;
import no.soprasteria.kaffebar.rapport.RapportRepository;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.stream.Collectors;

import static org.assertj.core.api.Assertions.assertThat;

class RapportTest {

    private static final Logger LOG = LoggerFactory.getLogger(RapportTest.class);

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
    void nativeQueryGirTopp3PerBrenneriPerMaaned() {
        try (var em = emf.createEntityManager()) {
            var rapport = new RapportRepository(em).toppBonnerMedNativeQuery();
            skrivUt(rapport);

            assertThat(QueryCountLogger.getSelectCount()).as("Antall SELECT-er").isEqualTo(1);
            assertThat(rapport).containsExactlyElementsOf(fasit());
        }
    }

    @Test
    void jdbcGirTopp3PerBrenneriPerMaaned() {
        try (var em = emf.createEntityManager()) {
            var rapport = new RapportRepository(em).toppBonnerMedJdbc();
            skrivUt(rapport);

            assertThat(rapport).containsExactlyElementsOf(fasit());
        }
    }

    private static void skrivUt(List<RapportRad> rapport) {
        var tabell = new StringBuilder();
        rapport.forEach(rad -> tabell.append("%-22s %s  %d. %-26s %3d stk  %8s kr%n".formatted(
                rad.brenneri(), rad.maaned(), rad.plassering(), rad.kaffebonne(), rad.antallSolgt(), rad.omsetning())));
        LOG.info("Rapport:\n{}{}", tabell, QueryCountLogger.oppsummering());
    }

    /**
     * Den «naive» ORM-måten: last alle linjene inn i minnet og regn ut rapporten i Java.
     * Greit som fasit i en test – men tenk på hva dette koster med en million linjer.
     */
    private static List<RapportRad> fasit() {
        record Nokkel(String brenneri, LocalDate maaned, String kaffebonne) {
        }
        record Sum(long antall, BigDecimal omsetning) {
            Sum pluss(Sum annen) {
                return new Sum(antall + annen.antall, omsetning.add(annen.omsetning));
            }
        }

        List<Bestillingslinje> linjer;
        try (var em = emf.createEntityManager()) {
            linjer = em.createQuery("""
                    SELECT l FROM Bestillingslinje l
                    JOIN FETCH l.bestilling
                    JOIN FETCH l.kaffebonne k
                    JOIN FETCH k.brenneri""", Bestillingslinje.class).getResultList();
        }

        var summer = linjer.stream().collect(Collectors.toMap(
                l -> new Nokkel(
                        l.getKaffebonne().getBrenneri().getNavn(),
                        l.getBestilling().getTidspunkt().toLocalDate().withDayOfMonth(1),
                        l.getKaffebonne().getNavn()),
                l -> new Sum(l.getAntall(), l.getLinjesum()),
                Sum::pluss));

        var mestSolgt = Comparator.<java.util.Map.Entry<Nokkel, Sum>>comparingLong(e -> e.getValue().antall()).reversed()
                .thenComparing(e -> e.getValue().omsetning(), Comparator.reverseOrder())
                .thenComparing(e -> e.getKey().kaffebonne());

        var grupper = summer.entrySet().stream().collect(Collectors.groupingBy(
                e -> new Nokkel(e.getKey().brenneri(), e.getKey().maaned(), null)));

        var fasit = new ArrayList<RapportRad>();
        grupper.values().forEach(gruppe -> {
            var topp3 = gruppe.stream().sorted(mestSolgt).limit(3).toList();
            for (int i = 0; i < topp3.size(); i++) {
                var e = topp3.get(i);
                fasit.add(new RapportRad(e.getKey().brenneri(), e.getKey().maaned(), i + 1,
                        e.getKey().kaffebonne(), e.getValue().antall(), e.getValue().omsetning()));
            }
        });
        fasit.sort(Comparator.comparing(RapportRad::brenneri)
                .thenComparing(RapportRad::maaned)
                .thenComparingInt(RapportRad::plassering));
        return fasit;
    }
}
