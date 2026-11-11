package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;

/**
 * Gir hver testklasse sin egen bestilling, så testene ikke påvirker hverandre.
 */
final class Testbestillinger {

    static final int KOLLISJON = 10;
    static final int KOLLISJON_API = 11;
    static final int KOLLISJON_API_UTEN_VERSJON = 12;

    private Testbestillinger() {
    }

    /**
     * Id-en til bestilling nummer {@code nr} (0-indeksert), sortert på id.
     */
    static Long id(EntityManagerFactory emf, int nr) {
        try (var em = emf.createEntityManager()) {
            return em.createQuery("SELECT b.id FROM Bestilling b ORDER BY b.id", Long.class)
                    .setFirstResult(nr)
                    .setMaxResults(1)
                    .getSingleResult();
        }
    }
}
