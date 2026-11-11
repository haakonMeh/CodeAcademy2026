package no.soprasteria.kaffebar.kvittering;

import no.soprasteria.kaffebar.domene.Bestilling;

import java.util.List;

public class Kvitteringsskriver {

    /**
     * Lager en tekstlig kvittering for alle bestillingene. Format:
     *
     * <pre>
     * Bestilling 1 – Kari Nordmann
     *   2 x Gatomboya (Tim Wendelboe)
     *   1 x Bombe (Supreme Roastworks)
     * Bestilling 2 – Ola Hansen
     *   3 x Kiambu (Jacu Coffee Roastery)
     * </pre>
     */
    public String lagKvittering(List<Bestilling> bestillinger) {
        var kvittering = new StringBuilder();
        for (var bestilling : bestillinger) {
            kvittering.append("Bestilling %d – %s%n".formatted(bestilling.getId(), bestilling.getKundenavn()));
            for (var linje : bestilling.getLinjer()) {
                var kaffebonne = linje.getKaffebonne();
                kvittering.append("  %d x %s (%s)%n".formatted(
                        linje.getAntall(), kaffebonne.getNavn(), kaffebonne.getBrenneri().getNavn()));
            }
        }
        return kvittering.toString();
    }

    /**
     * Som {@link #lagKvittering(List)}, men med tilpasningene etter linjene:
     *
     * <pre>
     * Bestilling 1 – Kari Nordmann
     *   2 x Gatomboya (Tim Wendelboe)
     *   + Havremelk
     *   + Ekstra shot
     * </pre>
     */
    public String lagKvitteringMedTilpasninger(List<Bestilling> bestillinger) {
        var kvittering = new StringBuilder();

        // TODO Nivå 2, oppgave B

        return kvittering.toString();
    }
}
