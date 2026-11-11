package no.soprasteria.kaffebar.rapport;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * Én rad i dashboardet «topp 3 kaffebønner per brenneri per måned».
 *
 * @param brenneri    navnet på brenneriet
 * @param maaned      første dag i måneden, f.eks. 2026-07-01
 * @param plassering  1, 2 eller 3 innen brenneri og måned
 * @param kaffebonne  navnet på kaffebønnen
 * @param antallSolgt summen av {@code antall} på alle bestillingslinjer for bønnen i måneden
 * @param omsetning   summen av {@code antall * enhetspris} for bønnen i måneden
 */
public record RapportRad(
        String brenneri,
        LocalDate maaned,
        int plassering,
        String kaffebonne,
        long antallSolgt,
        BigDecimal omsetning) {
}
