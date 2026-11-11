package no.soprasteria.kaffebar.domene;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;

@Entity
public class Brenneri {

    @Id
    @GeneratedValue
    private Long id;

    private String navn;

    private String sted;

    protected Brenneri() {
        // Påkrevd av JPA
    }

    public Brenneri(String navn, String sted) {
        this.navn = navn;
        this.sted = sted;
    }

    public Long getId() {
        return id;
    }

    public String getNavn() {
        return navn;
    }

    public String getSted() {
        return sted;
    }
}
