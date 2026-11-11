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
     * Henter alle bestillinger i databasen.
     */
    public List<Bestilling> hentAlleBestillinger() {
        // TODO Nivå 1: Hent alle bestillinger med JPQL: SELECT b FROM Bestilling b
        throw new UnsupportedOperationException("Ikke implementert ennå");
    }
}
