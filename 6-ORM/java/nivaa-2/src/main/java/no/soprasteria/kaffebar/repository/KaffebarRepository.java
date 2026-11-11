package no.soprasteria.kaffebar.repository;

import jakarta.persistence.EntityManager;
import no.soprasteria.kaffebar.domene.Bestilling;

import java.util.List;

public class KaffebarRepository {

    private final EntityManager em;

    public KaffebarRepository(EntityManager em) {
        this.em = em;
    }

    /**
     * Henter alle bestillinger med linjer, kaffebønner og brennerier.
     */
    public List<Bestilling> hentAlleBestillinger() {
        // TODO Nivå 2, oppgave A: Hent alt kvitteringen trenger i én spørring
        return em.createQuery("SELECT b FROM Bestilling b", Bestilling.class)
                .getResultList();
    }

    /**
     * Henter alle bestillinger med linjer, kaffebønner, brennerier og tilpasninger.
     */
    public List<Bestilling> hentBestillingerMedTilpasninger() {
        // TODO Nivå 2, oppgave B
        throw new UnsupportedOperationException("Ikke implementert ennå");
    }
}
