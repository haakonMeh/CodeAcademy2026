package no.soprasteria.kaffebar.domene;

import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

import java.math.BigDecimal;

@Entity
public class Bestillingslinje {

    @Id
    @GeneratedValue
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    private Bestilling bestilling;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    private Kaffebonne kaffebonne;

    private int antall;

    private BigDecimal enhetspris;

    protected Bestillingslinje() {
        // Påkrevd av JPA
    }

    public Bestillingslinje(Kaffebonne kaffebonne, int antall, BigDecimal enhetspris) {
        this.kaffebonne = kaffebonne;
        this.antall = antall;
        this.enhetspris = enhetspris;
    }

    void setBestilling(Bestilling bestilling) {
        this.bestilling = bestilling;
    }

    public BigDecimal getLinjesum() {
        return enhetspris.multiply(BigDecimal.valueOf(antall));
    }

    public Long getId() {
        return id;
    }

    public Bestilling getBestilling() {
        return bestilling;
    }

    public Kaffebonne getKaffebonne() {
        return kaffebonne;
    }

    public int getAntall() {
        return antall;
    }

    public BigDecimal getEnhetspris() {
        return enhetspris;
    }
}
