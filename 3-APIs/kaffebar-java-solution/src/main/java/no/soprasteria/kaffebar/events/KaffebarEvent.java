package no.soprasteria.kaffebar.events;

import com.fasterxml.jackson.annotation.JsonInclude;
import no.soprasteria.kaffebar.model.Order;
import no.soprasteria.kaffebar.model.OrderStatus;

/**
 * Meldingsformatet på exchangen. Bit for bit likt det
 * 4-Frontend/kaffebar-api/src/lib/events.ts publiserer, slik at
 * {@code /api/events}-route handleren i kaffebar-web ikke merker forskjell på hvilken
 * implementasjon som står bak.
 */
@JsonInclude(JsonInclude.Include.NON_NULL)
public record KaffebarEvent(String type, String timestamp, Order data, OrderStatus previousStatus) {
}
