package no.soprasteria.kaffebar.controller;

import no.soprasteria.kaffebar.api.OrdersApi;
import no.soprasteria.kaffebar.error.NotFoundException;
import no.soprasteria.kaffebar.events.OrderEventPublisher;
import no.soprasteria.kaffebar.model.Coffee;
import no.soprasteria.kaffebar.model.CreateOrderRequest;
import no.soprasteria.kaffebar.model.Order;
import no.soprasteria.kaffebar.model.OrderStatus;
import no.soprasteria.kaffebar.model.UpdateOrderStatusRequest;
import no.soprasteria.kaffebar.store.Menu;
import no.soprasteria.kaffebar.store.OrderStore;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RestController;

import java.net.URI;
import java.util.List;
import java.util.UUID;

/**
 * Implementerer det genererte {@code OrdersApi}-interfacet — oppgave 1, 5, 7 og 8.
 *
 * Legg merke til hvor lite som står her. Ruting, media types, binding av
 * query-parametere med default-verdier, og all validering kommer fra kontrakten via
 * det genererte interfacet. Det som er igjen er forretningslogikken. Det er poenget
 * med contract-first.
 */
@RestController
public class OrdersController implements OrdersApi {

    private final OrderStore store;
    private final OrderEventPublisher events;

    public OrdersController(OrderStore store, OrderEventPublisher events) {
        this.store = store;
        this.events = events;
    }

    /** Oppgave 8. limit og offset har allerede fått default-verdiene sine fra kontrakten. */
    @Override
    public ResponseEntity<List<Order>> listOrders(OrderStatus status, Integer limit, Integer offset) {
        return ResponseEntity.ok(store.list(status, limit, offset));
    }

    /** Oppgave 1. 201 med Location-header — det er det `Created` betyr. */
    @Override
    public ResponseEntity<Order> createOrder(CreateOrderRequest request) {
        Coffee coffee = Menu.find(request.getCoffeeId())
                // 404 og ikke 400: kontrakten er overholdt, ressursen finnes bare ikke.
                .orElseThrow(() -> new NotFoundException(
                        "Fant ingen kaffe med id " + request.getCoffeeId() + ". Se GET /menu."));

        Order order = store.create(request, coffee);
        events.orderCreated(order);

        return ResponseEntity
                .created(URI.create("/orders/" + order.getOrderId()))
                .body(order);
    }

    /** Oppgave 5 + 6. */
    @Override
    public ResponseEntity<Order> getOrder(UUID orderId) {
        Order order = store.find(orderId).orElseThrow(() -> notFound(orderId));
        return ResponseEntity.ok(order);
    }

    /** Oppgave 7, anbefalt variant. */
    @Override
    public ResponseEntity<Order> updateOrderStatus(UUID orderId, UpdateOrderStatusRequest request) {
        return ResponseEntity.ok(applyStatus(orderId, request.getStatus()));
    }

    /**
     * Oppgave 7, action-varianten. Samme implementasjon, to endepunkter — se FASIT.md
     * for hvorfor begge finnes og hvorfor PATCH er anbefalingen.
     */
    @Override
    public ResponseEntity<Order> setOrderStatus(UUID orderId, UpdateOrderStatusRequest request) {
        return ResponseEntity.ok(applyStatus(orderId, request.getStatus()));
    }

    private Order applyStatus(UUID orderId, OrderStatus status) {
        OrderStore.StatusChange change = store.setStatus(orderId, status)
                .orElseThrow(() -> notFound(orderId));

        // Publiser bare når noe faktisk endret seg. Ellers ville en frontend som setter
        // samme status to ganger lage støy på exchangen.
        if (change.previousStatus() != change.order().getStatus()) {
            events.orderStatusChanged(change.order(), change.previousStatus());
        }
        return change.order();
    }

    private static NotFoundException notFound(UUID orderId) {
        return new NotFoundException("Fant ingen bestilling med id " + orderId + ".");
    }
}
