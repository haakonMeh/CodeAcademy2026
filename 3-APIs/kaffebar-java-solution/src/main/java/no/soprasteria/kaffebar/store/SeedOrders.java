package no.soprasteria.kaffebar.store;

import no.soprasteria.kaffebar.model.CoffeeSize;
import no.soprasteria.kaffebar.model.MilkType;
import no.soprasteria.kaffebar.model.Order;
import no.soprasteria.kaffebar.model.OrderStatus;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.UUID;

/**
 * Fire ordrer i ulike statuser, slik at både kundeskjermen og baristakøen i samling 4
 * viser noe med én gang. Tidsstemplene settes relativt til oppstart, så «for 2
 * minutter siden» alltid stemmer.
 *
 * Samme fire ordrer, samme id-er og samme rekkefølge som
 * 4-Frontend/kaffebar-api/src/lib/seed.ts.
 */
final class SeedOrders {

    private SeedOrders() {
    }

    static List<Order> create(OffsetDateTime now) {
        return List.of(
                order("0de50001-0000-4000-8000-000000000001",
                        "c0ffee01-0000-4000-8000-000000000007", "Kaffe Latte", "Ada",
                        CoffeeSize.MEDIUM, MilkType.OAT, false, 1, OrderStatus.READY,
                        now.minusMinutes(12), now.minusMinutes(4)),
                order("0de50001-0000-4000-8000-000000000002",
                        "c0ffee01-0000-4000-8000-000000000006", "Cappuccino", "Kari",
                        CoffeeSize.LARGE, MilkType.WHOLE, true, 2, OrderStatus.BREWING,
                        now.minusMinutes(6), now.minusMinutes(2)),
                order("0de50001-0000-4000-8000-000000000003",
                        "c0ffee01-0000-4000-8000-000000000002", "Espresso", "Jonas",
                        CoffeeSize.SMALL, null, null, 1, OrderStatus.PENDING,
                        now.minusMinutes(3), now.minusMinutes(3)),
                order("0de50001-0000-4000-8000-000000000004",
                        "c0ffee01-0000-4000-8000-00000000000a", "Mocha", "Ingrid",
                        CoffeeSize.MEDIUM, MilkType.SOY, false, 3, OrderStatus.PENDING,
                        now.minusMinutes(1), now.minusMinutes(1)));
    }

    private static Order order(String orderId, String coffeeId, String coffeeName,
                               String customerName, CoffeeSize size, MilkType milkType,
                               Boolean extraShot, int quantity, OrderStatus status,
                               OffsetDateTime createdAt, OffsetDateTime updatedAt) {
        Order order = new Order(
                UUID.fromString(orderId), UUID.fromString(coffeeId), coffeeName,
                customerName, size, quantity, status, createdAt, updatedAt);
        order.setMilkType(milkType);
        order.setExtraShot(extraShot);
        return order;
    }
}
