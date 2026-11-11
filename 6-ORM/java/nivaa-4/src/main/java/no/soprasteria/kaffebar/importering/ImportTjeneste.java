package no.soprasteria.kaffebar.importering;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Kaffebonne;

import java.util.List;

/**
 * Importerer bestillinger fra nettbutikken. Skrevet av en kollega.
 */
public class ImportTjeneste {

    private final EntityManagerFactory emf;

    public ImportTjeneste(EntityManagerFactory emf) {
        this.emf = emf;
    }

    /**
     * @return antall importerte bestillinger
     */
    public int importer(List<NyBestilling> nyeBestillinger) {
        return emf.callInTransaction(em -> {
            for (var ny : nyeBestillinger) {
                var bestilling = new Bestilling(ny.kundenavn(), ny.tidspunkt());
                for (var linje : ny.linjer()) {
                    var kaffebonne = em.find(Kaffebonne.class, linje.kaffebonneId());
                    bestilling.leggTilLinje(new Bestillingslinje(kaffebonne, linje.antall(), linje.enhetspris()));
                }
                em.persist(bestilling);
            }
            return nyeBestillinger.size();
        });
    }
}
