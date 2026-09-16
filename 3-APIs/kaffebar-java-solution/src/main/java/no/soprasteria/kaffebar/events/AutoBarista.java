package no.soprasteria.kaffebar.events;

import no.soprasteria.kaffebar.model.Order;
import no.soprasteria.kaffebar.model.OrderStatus;
import no.soprasteria.kaffebar.store.OrderStore;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.scheduling.annotation.Scheduled;

/**
 * VALGFRITT — hører sammen med {@link OrderEventPublisher}.
 *
 * Flytter den eldste BREWING-ordren til READY, og deretter den eldste PENDING-ordren
 * til BREWING. Uten den skjer det ingenting i frontenden med mindre noen klikker, og
 * hele poenget med samling 4 — å se en ordre bevege seg i to vinduer samtidig — faller
 * bort.
 *
 * Hele klassen er betinget av {@code kaffebar.barista.auto-enabled}. Er den av,
 * finnes ikke beanen, og {@code @EnableScheduling} slår aldri inn.
 */
@Configuration
@EnableScheduling
@ConditionalOnProperty(prefix = "kaffebar.barista", name = "auto-enabled", havingValue = "true")
public class AutoBarista {

    private final OrderStore store;
    private final OrderEventPublisher events;

    public AutoBarista(OrderStore store, OrderEventPublisher events) {
        this.store = store;
        this.events = events;
    }

    @Scheduled(fixedDelayString = "${kaffebar.barista.interval-ms:8000}")
    void work() {
        advance(OrderStatus.BREWING, OrderStatus.READY);
        advance(OrderStatus.PENDING, OrderStatus.BREWING);
    }

    private void advance(OrderStatus from, OrderStatus to) {
        store.oldestWithStatus(from)
                .map(Order::getOrderId)
                .flatMap(orderId -> store.setStatus(orderId, to))
                .ifPresent(change -> events.orderStatusChanged(change.order(), change.previousStatus()));
    }
}
