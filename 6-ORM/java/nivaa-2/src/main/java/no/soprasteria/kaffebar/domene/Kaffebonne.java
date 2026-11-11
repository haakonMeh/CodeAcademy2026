package no.soprasteria.kaffebar.domene;

import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

@Entity
public class Kaffebonne {

    @Id
    @GeneratedValue
    private Long id;

    private String navn;

    private String opprinnelse;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    private Brenneri brenneri;

    protected Kaffebonne() {
        // Påkrevd av JPA
    }

    public Kaffebonne(String navn, String opprinnelse, Brenneri brenneri) {
        this.navn = navn;
        this.opprinnelse = opprinnelse;
        this.brenneri = brenneri;
    }

    public Long getId() {
        return id;
    }

    public String getNavn() {
        return navn;
    }

    public String getOpprinnelse() {
        return opprinnelse;
    }

    public Brenneri getBrenneri() {
        return brenneri;
    }
}
