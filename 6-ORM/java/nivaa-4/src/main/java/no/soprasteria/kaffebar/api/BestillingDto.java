package no.soprasteria.kaffebar.api;

import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Tilpasning;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;

/**
 * Det API-et viser frem av en bestilling. Frikoblet fra entiteten: ingen lazy-samlinger,
 * ingen sykliske referanser, og API-formatet endres ikke selv om databasemodellen gjør det.
 */
public record BestillingDto(
        Long id,
        String kundenavn,
        LocalDateTime tidspunkt,
        String status,
        BigDecimal totalpris,
        List<LinjeDto> linjer,
        List<String> tilpasninger) {

    public record LinjeDto(String kaffebonne, String brenneri, int antall, BigDecimal enhetspris) {

        static LinjeDto fra(Bestillingslinje linje) {
            var kaffebonne = linje.getKaffebonne();
            return new LinjeDto(kaffebonne.getNavn(), kaffebonne.getBrenneri().getNavn(),
                    linje.getAntall(), linje.getEnhetspris());
        }
    }

    /**
     * Må kalles mens EntityManager er åpen, siden den navigerer i lazy-relasjoner.
     */
    public static BestillingDto fra(Bestilling bestilling) {
        return new BestillingDto(
                bestilling.getId(),
                bestilling.getKundenavn(),
                bestilling.getTidspunkt(),
                bestilling.getStatus().name(),
                bestilling.getTotalpris(),
                bestilling.getLinjer().stream().map(LinjeDto::fra).toList(),
                bestilling.getTilpasninger().stream().map(Tilpasning::getBeskrivelse).toList());
    }
}
