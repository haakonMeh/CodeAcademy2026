package no.soprasteria.kaffebar.store;

import no.soprasteria.kaffebar.model.Coffee;
import no.soprasteria.kaffebar.model.CreateOrderRequest;
import no.soprasteria.kaffebar.model.Order;
import no.soprasteria.kaffebar.model.OrderStatus;
import org.springframework.stereotype.Component;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.temporal.ChronoUnit;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Lagring i minne.
 *
 * Bevisst valg: ingen database, ingen JPA, ingen Flyway. API-et starter rent hver
 * gang, og ingen bruker workshoptid på en migrasjon som feiler eller en container som
 * ikke kommer opp. Datasettet er lite nok til å ligge i en Map, og at alt nullstilles
 * ved omstart er en fordel — du kommer alltid tilbake til et kjent utgangspunkt.
 *
 * {@link ConcurrentHashMap} og ikke {@code HashMap}: Tomcat kjører hver request i sin
 * egen tråd, så dette er delt, foranderlig tilstand. Det er den eneste
 * trådsikkerhets-detaljen i hele prosjektet, og den er verdt å peke på.
 */
@Component
public class OrderStore {

    private final Map<UUID, Order> orders = new ConcurrentHashMap<>();

    public OrderStore() {
        reset();
    }

    /** Nyeste først, filtrert på status og paginert. Oppgave 8. */
    public List<Order> list(OrderStatus status, int limit, int offset) {
        return orders.values().stream()
                .filter(order -> status == null || order.getStatus() == status)
                .sorted(Comparator.comparing(Order::getCreatedAt).reversed())
                .skip(offset)
                .limit(limit)
                .toList();
    }

    public Optional<Order> find(UUID orderId) {
        return Optional.ofNullable(orders.get(orderId));
    }

    /** Oppgave 1: request inn, Order ut. To ulike typer, med vilje. */
    public Order create(CreateOrderRequest request, Coffee coffee) {
        OffsetDateTime now = now();

        Order order = new Order(
                UUID.randomUUID(),
                coffee.getId(),
                coffee.getName(),
                request.getCustomerName().trim(),
                request.getSize(),
                request.getQuantity(),
                OrderStatus.PENDING,
                now,
                now);
        order.setMilkType(request.getMilkType());
        order.setExtraShot(request.getExtraShot());

        orders.put(order.getOrderId(), order);
        return order;
    }

    /**
     * Setter status og returnerer både den oppdaterte ordren og forrige status, slik
     * at kalleren kan legge {@code previousStatus} på hendelsen — og la være å
     * publisere når ingenting faktisk endret seg.
     */
    public Optional<StatusChange> setStatus(UUID orderId, OrderStatus status) {
        return find(orderId).map(order -> {
            OrderStatus previous = order.getStatus();
            order.setStatus(status);
            order.setUpdatedAt(now());
            return new StatusChange(order, previous);
        });
    }

    /** Eldste ordre med en gitt status. Brukes av den valgfrie auto-baristaen. */
    public Optional<Order> oldestWithStatus(OrderStatus status) {
        return orders.values().stream()
                .filter(order -> order.getStatus() == status)
                .min(Comparator.comparing(Order::getCreatedAt));
    }

    /** Tømmer og seeder på nytt. Brukes av testene. */
    public final void reset() {
        orders.clear();
        for (Order order : SeedOrders.create(now())) {
            orders.put(order.getOrderId(), order);
        }
    }

    /**
     * Millisekundpresisjon, ikke nanosekunder. Jackson skriver OffsetDateTime med den
     * presisjonen den faktisk har, og `2026-09-13T09:41:12.004521Z` ser rart ut ved
     * siden av TypeScript-fasitens `2026-09-13T09:41:12.004Z`. Begge er gyldig
     * ISO-8601, men like er bedre enn nesten like.
     */
    static OffsetDateTime now() {
        return OffsetDateTime.now(ZoneOffset.UTC).truncatedTo(ChronoUnit.MILLIS);
    }

    public record StatusChange(Order order, OrderStatus previousStatus) {
    }
}
