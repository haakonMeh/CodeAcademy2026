import type { Coffee, CreateOrderRequest, Order, OrderStatus } from "@/types/domain";

/**
 * Mock-modus: en miniversjon av kaffebar-api som kjører inne i Next-prosessen.
 * Samme meny, samme statusflyt, samme auto-barista — bare uten containere.
 *
 * Se README: dette er nødløsningen, ikke standardvalget.
 */

export const MOCK_MENU: Coffee[] = [
  { id: "c0ffee01-0000-4000-8000-000000000001", name: "Filterkaffe", price: 39 },
  { id: "c0ffee01-0000-4000-8000-000000000002", name: "Espresso", price: 35 },
  { id: "c0ffee01-0000-4000-8000-000000000003", name: "Dobbel espresso", price: 45 },
  { id: "c0ffee01-0000-4000-8000-000000000004", name: "Americano", price: 42 },
  { id: "c0ffee01-0000-4000-8000-000000000005", name: "Cortado", price: 47 },
  { id: "c0ffee01-0000-4000-8000-000000000006", name: "Cappuccino", price: 49 },
  { id: "c0ffee01-0000-4000-8000-000000000007", name: "Kaffe Latte", price: 52 },
  { id: "c0ffee01-0000-4000-8000-000000000008", name: "Flat White", price: 55 },
  { id: "c0ffee01-0000-4000-8000-000000000009", name: "Chai Latte", price: 54 },
  { id: "c0ffee01-0000-4000-8000-00000000000a", name: "Mocha", price: 59 },
];

const minutesAgo = (minutes: number) => new Date(Date.now() - minutes * 60_000).toISOString();

// Samme grunn som bussen i events.ts: Next bunter instrumentation og route
// handlere hver for seg, så lageret må ligge på globalThis for å være ett lager.
const globalForStore = globalThis as typeof globalThis & {
  __kaffebarMockOrders?: Map<string, Order>;
};

const orders = (globalForStore.__kaffebarMockOrders ??= new Map<string, Order>());

const SEED = [
  {
    orderId: "0de50001-0000-4000-8000-000000000001",
    coffeeId: MOCK_MENU[6].id,
    coffeeName: "Kaffe Latte",
    customerName: "Ada",
    size: "MEDIUM",
    milkType: "OAT",
    extraShot: false,
    quantity: 1,
    status: "READY",
    createdAt: minutesAgo(12),
    updatedAt: minutesAgo(4),
  },
  {
    orderId: "0de50001-0000-4000-8000-000000000002",
    coffeeId: MOCK_MENU[5].id,
    coffeeName: "Cappuccino",
    customerName: "Kari",
    size: "LARGE",
    milkType: "WHOLE",
    extraShot: true,
    quantity: 2,
    status: "BREWING",
    createdAt: minutesAgo(6),
    updatedAt: minutesAgo(2),
  },
  {
    orderId: "0de50001-0000-4000-8000-000000000003",
    coffeeId: MOCK_MENU[1].id,
    coffeeName: "Espresso",
    customerName: "Jonas",
    size: "SMALL",
    quantity: 1,
    status: "PENDING",
    createdAt: minutesAgo(3),
    updatedAt: minutesAgo(3),
  },
] satisfies Order[];

// Bare første gang — ellers ville et nytt bunt nullstilt statusene baristaen
// allerede har flyttet på.
if (orders.size === 0) {
  for (const order of SEED) {
    orders.set(order.orderId, order);
  }
}

export function listMockOrders(params: { status?: OrderStatus; limit?: number } = {}): Order[] {
  return [...orders.values()]
    .filter((order) => (params.status ? order.status === params.status : true))
    .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
    .slice(0, params.limit ?? 100);
}

export function findMockOrder(orderId: string): Order | undefined {
  return orders.get(orderId);
}

export function createMockOrder(request: CreateOrderRequest): Order {
  const coffee = MOCK_MENU.find((item) => item.id === request.coffeeId);
  if (!coffee) throw new Error(`Ukjent coffeeId: ${request.coffeeId}`);

  const now = new Date().toISOString();
  const order: Order = {
    orderId: crypto.randomUUID(),
    coffeeId: coffee.id,
    coffeeName: coffee.name,
    customerName: request.customerName.trim(),
    size: request.size,
    ...(request.milkType ? { milkType: request.milkType } : {}),
    ...(request.extraShot !== undefined ? { extraShot: request.extraShot } : {}),
    quantity: request.quantity ?? 1,
    status: "PENDING",
    createdAt: now,
    updatedAt: now,
  };
  orders.set(order.orderId, order);
  return order;
}

export function setMockStatus(
  orderId: string,
  status: OrderStatus,
): { order: Order; previousStatus: OrderStatus } | undefined {
  const existing = orders.get(orderId);
  if (!existing) return undefined;

  const previousStatus = existing.status;
  const order: Order = { ...existing, status, updatedAt: new Date().toISOString() };
  orders.set(orderId, order);
  return { order, previousStatus };
}

export function oldestMockWithStatus(status: OrderStatus): Order | undefined {
  return [...orders.values()]
    .filter((order) => order.status === status)
    .sort((a, b) => Date.parse(a.createdAt) - Date.parse(b.createdAt))[0];
}
