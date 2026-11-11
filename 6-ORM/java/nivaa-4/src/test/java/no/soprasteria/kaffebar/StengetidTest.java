package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.tjeneste.StengetidTjeneste;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Nivå 3, oppgave C (bonus): Stengetid.
 */
class StengetidTest {

    private static EntityManagerFactory emf;

    @BeforeAll
    static void startDatabase() {
        emf = Database.start();
    }

    @Test
    void klareBestillingerBlirHentetOgFaarKompensasjon() {
        var klare = markerSomKlare(40, 41, 42, 43, 44);
        var prisFoer = totalpriser(klare);

        new StengetidTjeneste(emf).stengKassen();

        try (var em = emf.createEntityManager()) {
            for (var id : klare) {
                var bestilling = em.find(Bestilling.class, id);
                assertThat(bestilling.getStatus())
                        .as("Status for bestilling %d etter stengetid", id)
                        .isEqualTo(Bestilling.Status.HENTET);
                assertThat(bestilling.getTotalpris())
                        .as("Totalpris for bestilling %d etter stengetid", id)
                        .isEqualByComparingTo(prisFoer.get(id).subtract(StengetidTjeneste.KOMPENSASJON));
            }
        }
    }

    /**
     * Setter status KLAR på bestillingene med de gitte posisjonene (0-indeksert, sortert på id).
     */
    private static List<Long> markerSomKlare(int... posisjoner) {
        return emf.callInTransaction(em -> {
            var alle = em.createQuery("SELECT b FROM Bestilling b ORDER BY b.id", Bestilling.class).getResultList();
            return Arrays.stream(posisjoner)
                    .mapToObj(alle::get)
                    .peek(b -> b.setStatus(Bestilling.Status.KLAR))
                    .map(Bestilling::getId)
                    .toList();
        });
    }

    private static Map<Long, BigDecimal> totalpriser(List<Long> ider) {
        try (var em = emf.createEntityManager()) {
            return ider.stream().collect(Collectors.toMap(id -> id, id -> em.find(Bestilling.class, id).getTotalpris()));
        }
    }
}
