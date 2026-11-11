package no.soprasteria.kaffebar.tjeneste;

import jakarta.persistence.EntityManager;
import jakarta.persistence.EntityManagerFactory;
import jakarta.persistence.OptimisticLockException;
import jakarta.ws.rs.NotFoundException;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Kaffebonne;

import java.time.LocalDateTime;
import java.util.List;
import java.util.function.Consumer;

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
                // getReference gir en proxy uten SELECT – vi trenger bare fremmednøkkelen
                var kaffebonne = em.getReference(Kaffebonne.class, ny.kaffebonneId());
                // Bestillingslinje.bestilling er eiersiden (Bestilling.linjer har mappedBy), og det er den
                // Hibernate ser på når fremmednøkkelen skrives. getLinjer().add(...) setter bare den andre
                // siden. leggTilLinje() setter begge sider og holder totalprisen riktig.
                bestilling.leggTilLinje(new Bestillingslinje(kaffebonne, ny.antall(), ny.enhetspris()));
            }
            em.persist(bestilling); // cascade = ALL tar med linjene
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
            // em.remove(linje) alene ble ignorert i stillhet: linjen lå fortsatt i bestilling.linjer, og
            // cascade = ALL gjør at flush «persisterer» den igjen (en fjernet entitet som persisteres blir
            // managed på nytt, ifølge JPA-spesifikasjonen). Fjern den fra samlingen i stedet –
            // orphanRemoval = true sletter raden, og domenemetoden oppdaterer totalprisen.
            bestilling.fjernLinje(linje);
        });
    }

    /**
     * Lagrer endringer på en bestilling som kassa-appen har hentet tidligere og sender inn igjen.
     */
    public void lagre(Bestilling bestilling) {
        // Objektet kommer fra en EntityManager som er lukket, så det er «detached». Den nye
        // EntityManageren kjenner det ikke, og dirty checking gjelder bare managed entiteter.
        //
        // merge() kopierer tilstanden over på en managed instans (hentes fra databasen ved behov)
        // og returnerer den. Argumentet forblir detached – videre endringer må gjøres på returverdien.
        //
        // Ulempe: merge() skriver tilbake *alle* felt, også de kassa-appen ikke skulle endre.
        // Ofte er det bedre å sende inn bare endringen (f.eks. ny status) og gjøre den på en
        // nyhentet, managed entitet.
        try (var em = emf.createEntityManager()) {
            em.getTransaction().begin();
            em.merge(bestilling);
            em.getTransaction().commit();
        }
    }

    /**
     * Markerer bestillingen som hentet.
     *
     * @param forventetVersjon versjonen klienten så sist, eller {@code null} for å hoppe over sjekken
     */
    public void markerSomHentet(Long id, Long forventetVersjon) {
        iTransaksjon(em -> {
            var bestilling = em.find(Bestilling.class, id);
            if (bestilling == null) {
                throw new NotFoundException("Fant ikke bestilling " + id);
            }

            if (forventetVersjon != null) {
                var versjon = emf.getPersistenceUnitUtil().getVersion(bestilling);
                if (!forventetVersjon.equals(versjon)) {
                    throw new OptimisticLockException(
                            "Bestilling %d har versjon %s, klienten forventet %d".formatted(id, versjon, forventetVersjon),
                            null, bestilling);
                }
            }

            bestilling.setStatus(Bestilling.Status.HENTET);
        });
    }

    private void iTransaksjon(Consumer<EntityManager> arbeid) {
        try (var em = emf.createEntityManager()) {
            var transaksjon = em.getTransaction();
            transaksjon.begin();
            try {
                arbeid.accept(em);
                transaksjon.commit();
            } catch (RuntimeException e) {
                if (transaksjon.isActive()) {
                    transaksjon.rollback();
                }
                throw e;
            }
        }
    }
}
