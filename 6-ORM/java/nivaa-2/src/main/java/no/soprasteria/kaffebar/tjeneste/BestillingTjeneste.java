package no.soprasteria.kaffebar.tjeneste;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Kaffebonne;

import java.time.LocalDateTime;
import java.util.List;

/**
 * Brukes av kassa-appen. Skrevet av en kollega.
 */
public class BestillingTjeneste {

    private final EntityManagerFactory emf;

    public BestillingTjeneste(EntityManagerFactory emf) {
        this.emf = emf;
    }

    /**
     * Registrerer en ny bestilling og returnerer id-en.
     */
    public Long registrer(String kundenavn, List<NyLinje> linjer) {
        return emf.callInTransaction(em -> {
            var bestilling = new Bestilling(kundenavn, LocalDateTime.now());
            for (var ny : linjer) {
                var kaffebonne = em.find(Kaffebonne.class, ny.kaffebonneId());
                bestilling.getLinjer().add(new Bestillingslinje(kaffebonne, ny.antall(), ny.enhetspris()));
            }
            em.persist(bestilling);
            return bestilling.getId();
        });
    }

    /**
     * Kunden angrer på én av linjene.
     */
    public void fjernLinje(Long bestillingId, Long linjeId) {
        emf.runInTransaction(em -> {
            var bestilling = em.find(Bestilling.class, bestillingId);
            var linje = bestilling.getLinjer().stream()
                    .filter(l -> l.getId().equals(linjeId))
                    .findFirst()
                    .orElseThrow();
            em.remove(linje);
        });
    }
}
