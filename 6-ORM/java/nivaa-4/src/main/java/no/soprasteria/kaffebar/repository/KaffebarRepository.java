package no.soprasteria.kaffebar.repository;

import jakarta.persistence.EntityManager;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Kaffebonne;

import java.util.List;
import java.util.Optional;

public class KaffebarRepository {

    private final EntityManager em;

    public KaffebarRepository(EntityManager em) {
        this.em = em;
    }

    /**
     * Henter alle bestillinger med linjer, kaffebønner og brennerier.
     */
    public List<Bestilling> hentAlleBestillinger() {
        // Hibernate 6+ fjerner duplikate rotentiteter selv, så DISTINCT trengs ikke lenger
        return em.createQuery("""
                        SELECT b FROM Bestilling b
                        JOIN FETCH b.linjer l
                        JOIN FETCH l.kaffebonne k
                        JOIN FETCH k.brenneri
                        ORDER BY b.id""", Bestilling.class)
                .getResultList();
    }

    /**
     * Henter én bestilling med alt API-et trenger: linjer, kaffebønner, brennerier og tilpasninger.
     */
    public Optional<Bestilling> hentBestilling(Long id) {
        var bestilling = em.createQuery("""
                        SELECT b FROM Bestilling b
                        LEFT JOIN FETCH b.linjer l
                        LEFT JOIN FETCH l.kaffebonne k
                        LEFT JOIN FETCH k.brenneri
                        WHERE b.id = :id""", Bestilling.class)
                .setParameter("id", id)
                .getResultStream()
                .findFirst();
        bestilling.ifPresent(b -> em.createQuery(
                        "SELECT b FROM Bestilling b LEFT JOIN FETCH b.tilpasninger WHERE b = :bestilling", Bestilling.class)
                .setParameter("bestilling", b)
                .getSingleResult());
        return bestilling;
    }

    /**
     * Alternativ til {@link #hentAlleBestillinger()}: samme resultat, men fetch-planen er
     * skilt fra spørringen. Grafen kan gjenbrukes, eller defineres med {@code @NamedEntityGraph}.
     */
    public List<Bestilling> hentAlleBestillingerMedEntityGraph() {
        var graf = em.createEntityGraph(Bestilling.class);
        var linjer = graf.addSubgraph("linjer", Bestillingslinje.class);
        var kaffebonne = linjer.addSubgraph("kaffebonne", Kaffebonne.class);
        kaffebonne.addAttributeNodes("brenneri");

        return em.createQuery("SELECT b FROM Bestilling b ORDER BY b.id", Bestilling.class)
                .setHint("jakarta.persistence.fetchgraph", graf)
                .getResultList();
    }

    /**
     * Henter alle bestillinger med linjer, kaffebønner, brennerier og tilpasninger.
     */
    public List<Bestilling> hentBestillingerMedTilpasninger() {
        // JOIN FETCH på både linjer og tilpasninger gir MultipleBagFetchException:
        // to List-er («bags») gir et kartesisk produkt som Hibernate ikke kan skille fra ekte duplikater.
        // Løsning: to spørringer. Den andre fyller ut tilpasningene på de samme (managed) bestillingene
        // som allerede ligger i persistence context.
        var bestillinger = hentAlleBestillinger();
        em.createQuery("SELECT b FROM Bestilling b LEFT JOIN FETCH b.tilpasninger", Bestilling.class)
                .getResultList();
        return bestillinger;
    }
}
