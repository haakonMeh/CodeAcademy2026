package no.soprasteria.kaffebar.kvittering;

import no.soprasteria.kaffebar.domene.Bestilling;

import java.util.List;

public class Kvitteringsskriver {

    /**
     * Lager en tekstlig kvittering for alle bestillingene. Forventet format:
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

        // TODO Nivå 1: Iterer over bestillingene og linjene deres.
        //  Naviger bestilling -> linje -> kaffebønne -> brenneri og bygg opp kvitteringen.

        return kvittering.toString();
    }
}
