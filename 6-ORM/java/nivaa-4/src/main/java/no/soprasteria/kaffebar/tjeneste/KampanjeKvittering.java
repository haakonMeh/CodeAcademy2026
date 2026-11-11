package no.soprasteria.kaffebar.tjeneste;

import java.math.BigDecimal;

/**
 * Det kampanje-appen får vist for en bestilling.
 */
public record KampanjeKvittering(
        Long bestillingId,
        String kundenavn,
        BigDecimal ordinaerPris,
        BigDecimal kampanjepris) {
}
