package no.soprasteria.kaffebar.store;

import no.soprasteria.kaffebar.model.Coffee;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Kaffemenyen.
 *
 * Faste uuid-er, ikke {@code UUID.randomUUID()} som i starteren. En deltaker skal
 * kunne skrive den samme {@code coffeeId}-en i et curl-kall i dag og i morgen, og
 * slides skal kunne vise ekte id-er uten at de blir feil ved neste omstart.
 *
 * Nøyaktig de samme ti varene, prisene og id-ene som
 * 4-Frontend/kaffebar-api/src/lib/seed.ts. Da stemmer eksemplene uansett hvilken
 * av de tre implementasjonene som kjører.
 */
public final class Menu {

    public static final List<Coffee> ITEMS = List.of(
            coffee("c0ffee01-0000-4000-8000-000000000001", "Filterkaffe", 39),
            coffee("c0ffee01-0000-4000-8000-000000000002", "Espresso", 35),
            coffee("c0ffee01-0000-4000-8000-000000000003", "Dobbel espresso", 45),
            coffee("c0ffee01-0000-4000-8000-000000000004", "Americano", 42),
            coffee("c0ffee01-0000-4000-8000-000000000005", "Cortado", 47),
            coffee("c0ffee01-0000-4000-8000-000000000006", "Cappuccino", 49),
            coffee("c0ffee01-0000-4000-8000-000000000007", "Kaffe Latte", 52),
            coffee("c0ffee01-0000-4000-8000-000000000008", "Flat White", 55),
            coffee("c0ffee01-0000-4000-8000-000000000009", "Chai Latte", 54),
            coffee("c0ffee01-0000-4000-8000-00000000000a", "Mocha", 59));

    private Menu() {
    }

    public static Optional<Coffee> find(UUID coffeeId) {
        return ITEMS.stream().filter(c -> c.getId().equals(coffeeId)).findFirst();
    }

    private static Coffee coffee(String id, String name, int price) {
        return new Coffee(UUID.fromString(id), name, new BigDecimal(price));
    }
}
