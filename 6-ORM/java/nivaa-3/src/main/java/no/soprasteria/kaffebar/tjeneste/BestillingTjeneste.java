package no.soprasteria.kaffebar.tjeneste;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Kaffebonne;

import java.time.LocalDateTime;
import java.util.List;

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
        try (var em = emf.createEntityManager()) {
            em.getTransaction().begin();
            // Hibernate oppdager endringer automatisk, så her trengs ikke noe mer
            em.getTransaction().commit();
        }
    }
}
