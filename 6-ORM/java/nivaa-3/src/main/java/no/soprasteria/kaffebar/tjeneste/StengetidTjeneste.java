package no.soprasteria.kaffebar.tjeneste;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;

import java.math.BigDecimal;

/**
 * Ved stengetid markeres alle bestillinger som står klare som hentet, og kundene får 10 kr i
 * kompensasjon for ventetiden. Skrevet av en kollega.
 */
public class StengetidTjeneste {

    public static final BigDecimal KOMPENSASJON = new BigDecimal("10.00");

    private final EntityManagerFactory emf;

    public StengetidTjeneste(EntityManagerFactory emf) {
        this.emf = emf;
    }

    /**
     * @return antall bestillinger som ble stengt
     */
    public int stengKassen() {
        return emf.callInTransaction(em -> {
            var klare = em.createQuery("SELECT b FROM Bestilling b WHERE b.status = :klar", Bestilling.class)
                    .setParameter("klar", Bestilling.Status.KLAR)
                    .getResultList();

            // Én UPDATE i stedet for én per bestilling – raskt!
            em.createQuery("UPDATE Bestilling b SET b.status = :hentet WHERE b.status = :klar")
                    .setParameter("hentet", Bestilling.Status.HENTET)
                    .setParameter("klar", Bestilling.Status.KLAR)
                    .executeUpdate();

            for (var bestilling : klare) {
                bestilling.setTotalpris(bestilling.getTotalpris().subtract(KOMPENSASJON));
            }
            return klare.size();
        });
    }
}
