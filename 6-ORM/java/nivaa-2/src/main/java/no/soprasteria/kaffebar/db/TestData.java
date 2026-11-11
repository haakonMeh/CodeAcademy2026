package no.soprasteria.kaffebar.db;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.domene.Bestilling;
import no.soprasteria.kaffebar.domene.Bestillingslinje;
import no.soprasteria.kaffebar.domene.Brenneri;
import no.soprasteria.kaffebar.domene.Kaffebonne;
import no.soprasteria.kaffebar.domene.Tilpasning;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Random;

/**
 * Fyller databasen med 5 brennerier, 15 kaffebønner og 100 bestillinger.
 * Dataene er deterministiske (fast seed), så alle i rommet får samme resultat.
 */
public final class TestData {

    public static final int ANTALL_BESTILLINGER = 100;

    private static final LocalDateTime START = LocalDateTime.of(2026, 7, 1, 7, 0);

    private static final List<String> FORNAVN = List.of(
            "Kari", "Ola", "Ingrid", "Lars", "Nora", "Jonas", "Sofie", "Emil",
            "Astrid", "Henrik", "Maja", "Sindre", "Thea", "Magnus", "Ida");

    private static final List<String> ETTERNAVN = List.of(
            "Nordmann", "Hansen", "Johansen", "Olsen", "Larsen", "Berg", "Haugen",
            "Dahl", "Lie", "Bakken");

    private static final List<String> TILPASNINGER = List.of(
            "Havremelk", "Ekstra shot", "Laktosefri melk", "Karamellsirup", "Ekstra varm");

    private TestData() {
    }

    public static void fyllDatabasen(EntityManagerFactory emf) {
        var random = new Random(42);
        // Egen seed for tilpasninger, slik at resten av dataene er identiske med nivå 1
        var tilpasningRandom = new Random(7);

        emf.runInTransaction(em -> {
            var wendelboe = new Brenneri("Tim Wendelboe", "Oslo");
            var supreme = new Brenneri("Supreme Roastworks", "Oslo");
            var kaffa = new Brenneri("Kaffa", "Oslo");
            var fuglen = new Brenneri("Fuglen", "Oslo");
            var jacu = new Brenneri("Jacu Coffee Roastery", "Ålesund");
            List.of(wendelboe, supreme, kaffa, fuglen, jacu).forEach(em::persist);

            var bonner = List.of(
                    new Kaffebonne("Gatomboya", "Kenya", wendelboe),
                    new Kaffebonne("El Puente", "Honduras", wendelboe),
                    new Kaffebonne("Nyeri Hill", "Kenya", wendelboe),
                    new Kaffebonne("Bombe", "Etiopia", supreme),
                    new Kaffebonne("La Esperanza", "Colombia", supreme),
                    new Kaffebonne("Karimikui", "Kenya", supreme),
                    new Kaffebonne("Daterra Sweet Collection", "Brasil", kaffa),
                    new Kaffebonne("Yirgacheffe Konga", "Etiopia", kaffa),
                    new Kaffebonne("Finca Tamana", "Colombia", kaffa),
                    new Kaffebonne("Huila Decaf", "Colombia", fuglen),
                    new Kaffebonne("Guji Hambela", "Etiopia", fuglen),
                    new Kaffebonne("Santa Barbara", "Guatemala", fuglen),
                    new Kaffebonne("Kiambu", "Kenya", jacu),
                    new Kaffebonne("Sidamo Bensa", "Etiopia", jacu),
                    new Kaffebonne("Cerrado Mineiro", "Brasil", jacu));
            bonner.forEach(em::persist);

            for (int i = 0; i < ANTALL_BESTILLINGER; i++) {
                var kunde = FORNAVN.get(random.nextInt(FORNAVN.size()))
                        + " " + ETTERNAVN.get(random.nextInt(ETTERNAVN.size()));
                var tidspunkt = START
                        .plusDays(random.nextInt(120))
                        .plusMinutes(random.nextInt(10 * 60));

                var bestilling = new Bestilling(kunde, tidspunkt);
                int antallLinjer = 1 + random.nextInt(4);
                for (int l = 0; l < antallLinjer; l++) {
                    var bonne = bonner.get(random.nextInt(bonner.size()));
                    var pris = BigDecimal.valueOf(45 + 5L * random.nextInt(6));
                    bestilling.leggTilLinje(new Bestillingslinje(bonne, 1 + random.nextInt(3), pris));
                }
                int antallTilpasninger = tilpasningRandom.nextInt(4);
                for (int t = 0; t < antallTilpasninger; t++) {
                    var beskrivelse = TILPASNINGER.get(tilpasningRandom.nextInt(TILPASNINGER.size()));
                    bestilling.leggTilTilpasning(new Tilpasning(beskrivelse, BigDecimal.valueOf(10)));
                }
                em.persist(bestilling);
            }
        });
    }
}
