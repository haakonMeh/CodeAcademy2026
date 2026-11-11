package no.soprasteria.kaffebar.tjeneste;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;

import java.math.BigDecimal;

/**
 * Ved stengetid markeres alle bestillinger som står klare som hentet, og kundene får 10 kr i
 * kompensasjon for ventetiden.
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
        // Feilen: en JPQL-UPDATE går rett til databasen, forbi persistence context. Bestillingene som var
        // lastet fra før, sto fortsatt som KLAR i minnet. Da totalprisen ble endret, ble de dirty, og ved
        // commit skrev Hibernate *hele* raden tilbake – inkludert den gamle statusen KLAR.
        //
        // Løsning: gjør alt i én bulk-UPDATE. Da er det ingen entiteter i minnet som kan bli utdaterte.
        // Alternativer:
        //  - kjør bulk-oppdateringen først og em.clear() / em.refresh() før entitetene brukes videre
        //  - dropp bulk og endre både status og pris på entitetene (dirty checking + JDBC-batching)
        //  - @DynamicUpdate skriver bare endrede kolonner og skjuler feilen her – men entitetene i minnet
        //    er fortsatt feil, så det er ingen ekte løsning
        return emf.callInTransaction(em -> em.createQuery("""
                        UPDATE Bestilling b
                        SET b.status = :hentet, b.totalpris = b.totalpris - :kompensasjon
                        WHERE b.status = :klar""")
                .setParameter("hentet", Bestilling.Status.HENTET)
                .setParameter("kompensasjon", KOMPENSASJON)
                .setParameter("klar", Bestilling.Status.KLAR)
                .executeUpdate());
    }
}
