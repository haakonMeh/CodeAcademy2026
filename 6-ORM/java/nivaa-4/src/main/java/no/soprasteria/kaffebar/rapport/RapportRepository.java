package no.soprasteria.kaffebar.rapport;

import jakarta.persistence.EntityManager;

import java.util.List;

/**
 * Dashboardet: de 3 mest solgte kaffebønnene per brenneri per måned, med omsetning.
 *
 * <p>Regler:
 * <ul>
 *   <li>Måneden er måneden til {@code Bestilling.tidspunkt}.</li>
 *   <li>Mest solgt = høyest {@code antallSolgt}. Ved likt antall: høyest omsetning, deretter kaffebønnens navn
 *       alfabetisk.</li>
 *   <li>Radene sorteres på brenneri, måned og plassering.</li>
 * </ul>
 */
public class RapportRepository {

    private final EntityManager em;

    public RapportRepository(EntityManager em) {
        this.em = em;
    }

    /**
     * Rapporten med {@code em.createNativeQuery(sql, Tuple.class)}.
     */
    public List<RapportRad> toppBonnerMedNativeQuery() {
        // TODO Nivå 4, oppgave B
        throw new UnsupportedOperationException("Ikke implementert ennå");
    }

    /**
     * Rapporten med ren JDBC via {@code em.unwrap(Session.class).doReturningWork(...)}.
     */
    public List<RapportRad> toppBonnerMedJdbc() {
        // TODO Nivå 4, oppgave B
        throw new UnsupportedOperationException("Ikke implementert ennå");
    }
}
