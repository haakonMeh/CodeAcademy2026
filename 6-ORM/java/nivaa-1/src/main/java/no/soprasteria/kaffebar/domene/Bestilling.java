package no.soprasteria.kaffebar.domene;

import jakarta.persistence.CascadeType;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Id;
import jakarta.persistence.OneToMany;
import jakarta.persistence.OrderBy;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
public class Bestilling {

    @Id
    @GeneratedValue
    private Long id;

    private String kundenavn;

    private LocalDateTime tidspunkt;

    @Enumerated(EnumType.STRING)
    private Status status = Status.MOTTATT;

    private BigDecimal totalpris = BigDecimal.ZERO;

    @OneToMany(mappedBy = "bestilling", cascade = CascadeType.ALL, orphanRemoval = true)
    @OrderBy("id")
    private List<Bestillingslinje> linjer = new ArrayList<>();

    protected Bestilling() {
        // Påkrevd av JPA
    }

    public Bestilling(String kundenavn, LocalDateTime tidspunkt) {
        this.kundenavn = kundenavn;
        this.tidspunkt = tidspunkt;
    }

    public void leggTilLinje(Bestillingslinje linje) {
        linje.setBestilling(this);
        linjer.add(linje);
        totalpris = totalpris.add(linje.getLinjesum());
    }

    public Long getId() {
        return id;
    }

    public String getKundenavn() {
        return kundenavn;
    }

    public LocalDateTime getTidspunkt() {
        return tidspunkt;
    }

    public Status getStatus() {
        return status;
    }

    public void setStatus(Status status) {
        this.status = status;
    }

    public BigDecimal getTotalpris() {
        return totalpris;
    }

    public void setTotalpris(BigDecimal totalpris) {
        this.totalpris = totalpris;
    }

    public List<Bestillingslinje> getLinjer() {
        return linjer;
    }

    public enum Status {
        MOTTATT, KLAR, HENTET
    }
}
