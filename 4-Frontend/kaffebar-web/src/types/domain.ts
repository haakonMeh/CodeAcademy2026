/**
 * Kortnavn for typene som kommer ut av kontrakten.
 *
 * `kaffebar.ts` er generert med `yarn generate:types` og skal ikke redigeres.
 * Denne fila er den eneste håndskrevne broen over til den, slik at resten av
 * appen kan skrive `Order` i stedet for
 * `components["schemas"]["Order"]`.
 */
import type { components } from "./kaffebar";

export type Coffee = components["schemas"]["Coffee"];
export type Order = components["schemas"]["Order"];
export type OrderStatus = components["schemas"]["OrderStatus"];
export type CoffeeSize = components["schemas"]["CoffeeSize"];
export type MilkType = components["schemas"]["MilkType"];
export type CreateOrderRequest = components["schemas"]["CreateOrderRequest"];
export type Problem = components["schemas"]["Problem"];

/** Kolonnene på baristaskjermen, i den rekkefølgen en ordre beveger seg. */
export const STATUS_ORDER = ["PENDING", "BREWING", "READY"] as const;

export const STATUS_LABEL: Record<OrderStatus, string> = {
  PENDING: "I kø",
  BREWING: "Under arbeid",
  READY: "Klar",
};

export const SIZE_LABEL: Record<CoffeeSize, string> = {
  SMALL: "Liten",
  MEDIUM: "Medium",
  LARGE: "Stor",
};

export const MILK_LABEL: Record<MilkType, string> = {
  WHOLE: "Helmelk",
  SKIMMED: "Skummet",
  OAT: "Havre",
  SOY: "Soya",
};

/** Hendelsene kaffebar-api publiserer på `kaffebar`-exchangen. */
export interface KaffebarEvent {
  type: "order.created" | "order.status-changed";
  timestamp: string;
  data: Order;
  previousStatus?: OrderStatus;
}
