package no.soprasteria.kaffebar.tjeneste;

import java.math.BigDecimal;

/**
 * En linje slik kassa-appen sender den inn.
 */
public record NyLinje(Long kaffebonneId, int antall, BigDecimal enhetspris) {
}
