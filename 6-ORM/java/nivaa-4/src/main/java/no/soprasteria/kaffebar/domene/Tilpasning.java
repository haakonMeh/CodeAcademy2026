package no.soprasteria.kaffebar.domene;

import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

import java.math.BigDecimal;

@Entity
public class Tilpasning {

    @Id
    @GeneratedValue
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    private Bestilling bestilling;

    private String beskrivelse;

    private BigDecimal tillegg;

    protected Tilpasning() {
        // Påkrevd av JPA
    }

    public Tilpasning(String beskrivelse, BigDecimal tillegg) {
        this.beskrivelse = beskrivelse;
        this.tillegg = tillegg;
    }

    void setBestilling(Bestilling bestilling) {
        this.bestilling = bestilling;
    }

    public Long getId() {
        return id;
    }

    public Bestilling getBestilling() {
        return bestilling;
    }

    public String getBeskrivelse() {
        return beskrivelse;
    }

    public BigDecimal getTillegg() {
        return tillegg;
    }
}
