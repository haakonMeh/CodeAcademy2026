package no.soprasteria.kaffebar.importering;

import no.soprasteria.kaffebar.tjeneste.NyLinje;

import java.time.LocalDateTime;
import java.util.List;

/**
 * En bestilling fra nettbutikken, slik den kommer i importfilen.
 */
public record NyBestilling(String kundenavn, LocalDateTime tidspunkt, List<NyLinje> linjer) {
}
