import { EventEmitter } from "node:events";
import type { EventHandler, Subscription } from "@/lib/rabbitmq";
import type { KaffebarEvent, Order, OrderStatus } from "@/types/domain";
import { oldestMockWithStatus, setMockStatus } from "./store";

/**
 * Mock-modus: en auto-barista og en hendelsesbuss som erstatter RabbitMQ.
 * Samme meldingsformat, samme oppførsel — bare i minnet.
 */

/**
 * Bussen MÅ ligge på globalThis.
 *
 * Next bunter instrumentation.ts og route handlerne hver for seg. Et vanlig
 * modulnivå-objekt blir da opprettet én gang per bunt, og hendelsene MSW-handleren
 * sender havner i en annen EventEmitter enn den /api/events lytter på. Symptomet
 * er at alt ser riktig ut, men ingenting beveger seg.
 */
const globalForBus = globalThis as typeof globalThis & { __kaffebarMockBus?: EventEmitter };

const bus = (globalForBus.__kaffebarMockBus ??= new EventEmitter());
bus.setMaxListeners(50);

const AUTO_BARISTA_INTERVAL_MS = 8_000;

export function emitMockEvent(event: KaffebarEvent): void {
  bus.emit("event", event);
}

export function emitMockOrderCreated(order: Order): void {
  emitMockEvent({ type: "order.created", timestamp: new Date().toISOString(), data: order });
}

export function emitMockStatusChanged(order: Order, previousStatus: OrderStatus): void {
  emitMockEvent({
    type: "order.status-changed",
    timestamp: new Date().toISOString(),
    data: order,
    previousStatus,
  });
}

/** Speiler subscribeToOrders(), men uten broker. */
export function subscribeToMockOrders(onEvent: EventHandler): Subscription {
  bus.on("event", onEvent);
  return {
    close: async () => {
      bus.off("event", onEvent);
    },
  };
}

const globalForTimer = globalThis as typeof globalThis & {
  __kaffebarMockBarista?: NodeJS.Timeout;
};

/** Flytter én ordre ett steg videre med jevne mellomrom, som fasit-API-et gjør. */
export function startMockBarista(): void {
  if (globalForTimer.__kaffebarMockBarista) return;

  const advance = (from: OrderStatus, to: OrderStatus) => {
    const candidate = oldestMockWithStatus(from);
    if (!candidate) return;
    const result = setMockStatus(candidate.orderId, to);
    if (result) emitMockStatusChanged(result.order, result.previousStatus);
  };

  const timer = setInterval(() => {
    advance("BREWING", "READY");
    advance("PENDING", "BREWING");
  }, AUTO_BARISTA_INTERVAL_MS);
  timer.unref();
  globalForTimer.__kaffebarMockBarista = timer;
}
