import { KAFFEBAR_API_URL, USE_MOCK_API } from "./config";
import type { Coffee, CreateOrderRequest, Order, OrderStatus, Problem } from "@/types/domain";

/**
 * HTTP-laget mot Kaffebar-API-et. UTLEVERT — du skal ikke skrive dette i dag.
 *
 * Å hente data over HTTP er ren rørlegging, og API-et er det dere bygget selv i
 * mai. Det er derfor ferdig her. Men bruk to minutter på å lese fila, for den
 * inneholder tre valg du kommer til å ta igjen i ditt eget prosjekt:
 *
 *   1. Typene er ikke skrevet for hånd. De faller ut av den samme kontrakten som
 *      drev API-et i mai — se `src/types/kaffebar.ts`, generert med
 *      `yarn generate:types`, og `src/types/domain.ts` som er den eneste
 *      håndskrevne broen over til den.
 *   2. Cachingen er et bevisst valg per endepunkt. Menyen tåler 60 sekunder;
 *      ordrene må aldri caches. Glemmer du `no-store`, ser du en fastfrosset side
 *      og lurer på hvorfor ingenting skjer.
 *   3. Feil pakkes ut fra RFC 7807 Problem Details ett sted, slik at meldingen
 *      API-et faktisk sendte når helt fram til brukeren. Prøv å bestille med et
 *      tomt navn og se hva som står på skjermen.
 *
 * Alt her kjører på serveren. Nettleseren snakker med Next-appen, Next-appen
 * snakker med Kaffebar-API-et — appen er sin egen BFF. Derfor er
 * KAFFEBAR_API_URL ikke prefikset med NEXT_PUBLIC_.
 *
 * Kjører du med USE_MOCK_API=true, går hver funksjon rett i minnelageret i
 * stedet for ut på nettverket. Se `src/mocks/api.ts`.
 */

/**
 * Kaffebar-API-et svarer med RFC 7807 Problem Details når noe er galt. Vi pakker
 * det ut ett sted, slik at kallene under kan være tre linjer hver.
 */
async function handle<T>(response: Response): Promise<T> {
  if (response.ok) {
    return (await response.json()) as T;
  }

  let detail = `${response.status} ${response.statusText}`;
  if (response.headers.get("content-type")?.includes("problem+json")) {
    const problem = (await response.json()) as Problem;
    // `errors` finnes bare på valideringsfeil, og er det mest nyttige å vise.
    const fields = problem.errors?.map((e) => `${e.field}: ${e.message}`).join(", ");
    detail = fields ? `${problem.title} — ${fields}` : (problem.detail ?? problem.title);
  }
  throw new Error(detail);
}

/** GET /menu */
export async function fetchMenu(): Promise<Coffee[]> {
  if (USE_MOCK_API) return (await import("@/mocks/api")).mockFetchMenu();

  // Menyen endrer seg ikke under en workshop. 60 sekunders cache er nok til at
  // et sideskifte føles umiddelbart, uten at den blir feil hvis du endrer seed.
  const response = await fetch(`${KAFFEBAR_API_URL}/menu`, { next: { revalidate: 60 } });
  return handle<Coffee[]>(response);
}

/** GET /orders?status=&limit=&offset= */
export async function fetchOrders(
  params: { status?: OrderStatus; limit?: number } = {},
): Promise<Order[]> {
  if (USE_MOCK_API) return (await import("@/mocks/api")).mockFetchOrders(params);

  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  query.set("limit", String(params.limit ?? 100));

  // Ordrene endrer seg hele tiden — aldri cache dem.
  const response = await fetch(`${KAFFEBAR_API_URL}/orders?${query}`, { cache: "no-store" });
  return handle<Order[]>(response);
}

/** POST /orders */
export async function createOrder(request: CreateOrderRequest): Promise<Order> {
  if (USE_MOCK_API) return (await import("@/mocks/api")).mockCreateOrder(request);

  const response = await fetch(`${KAFFEBAR_API_URL}/orders`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(request),
    cache: "no-store",
  });
  return handle<Order>(response);
}

/**
 * PATCH /orders/{orderId}
 *
 * PATCH og ikke PUT: vi endrer én egenskap på en ordre som finnes fra før.
 * (Fasit-API-et støtter også POST /orders/{id}/status for de som valgte den
 * varianten i samling 3 — se FASIT.md i 3-APIs/kaffebar-java-solution.)
 */
export async function updateOrderStatus(orderId: string, status: OrderStatus): Promise<Order> {
  if (USE_MOCK_API) {
    return (await import("@/mocks/api")).mockUpdateOrderStatus(orderId, status);
  }

  const response = await fetch(`${KAFFEBAR_API_URL}/orders/${orderId}`, {
    method: "PATCH",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ status }),
    cache: "no-store",
  });
  return handle<Order>(response);
}
