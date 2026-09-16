"use client";

import { useCallback, useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { useEventSource } from "./useEventSource";
import type { KaffebarEvent, Order, OrderStatus } from "@/types/domain";

/**
 * FASIT for ekstraoppgave 3 (event-storm) og 4 (optimistisk oppdatering).
 *
 * Den obligatoriske `useOrders.ts` gjør det enkleste som virker: hver hendelse
 * invaliderer cachen, og TanStack Query henter lista på nytt. Det er riktig sted
 * å begynne, og det holder helt til hendelsene kommer fortere enn du rekker å
 * hente.
 *
 * Denne fila viser to grep, og de løser hvert sitt problem:
 *
 *   1. Vi skriver hendelsen rett inn i cachen, så skjermen oppdaterer seg uten et
 *      eneste nettverkskall. Da må vi selv håndtere hendelser som kommer i feil
 *      rekkefølge, og det gjør vi med `updatedAt`.
 *   2. Vi henter likevel hele lista med jevne mellomrom, men maks én gang i
 *      sekundet uansett hvor mange hendelser som kommer. Det er sikkerhetsnettet
 *      vårt hvis vi skulle ha mistet en hendelse eller regnet feil.
 *
 * Kopier det du vil bruke inn i `useOrders.ts`. Det er ikke noe poeng i å ta med
 * begge deler i et ekte prosjekt før du vet at du trenger dem.
 */

export const ORDERS_QUERY_KEY = ["orders"] as const;

/** Maks én full henting i sekundet, uansett hvor mange hendelser som kommer. */
const REFETCH_INTERVAL_MS = 1_000;

async function fetchOrdersFromBff(): Promise<Order[]> {
  const response = await fetch("/api/orders", { cache: "no-store" });
  if (!response.ok) throw new Error("Klarte ikke å hente bestillinger");
  return response.json();
}

/**
 * Skriver én hendelse inn i cachen.
 *
 * `updatedAt` er vakten mot hendelser i feil rekkefølge: kommer det en eldre
 * versjon av en bestilling vi allerede har en nyere versjon av, kaster vi den.
 * Uten den linja kan en forsinket PENDING-hendelse flytte en bestilling som
 * allerede er READY tilbake til første kolonne.
 */
function applyEvent(orders: Order[] | undefined, event: KaffebarEvent): Order[] {
  const incoming = event.data;
  if (!orders) return [incoming];

  const existing = orders.find((order) => order.orderId === incoming.orderId);
  if (!existing) return [incoming, ...orders];

  if (Date.parse(incoming.updatedAt) < Date.parse(existing.updatedAt)) return orders;

  return orders.map((order) => (order.orderId === incoming.orderId ? incoming : order));
}

function parseEvent(message: MessageEvent): KaffebarEvent | undefined {
  try {
    return JSON.parse(message.data as string) as KaffebarEvent;
  } catch {
    // `ready` og heartbeat har ikke et ordre-objekt i seg. Det er helt greit.
    return undefined;
  }
}

/** Samler opp hendelser og henter lista på nytt maks én gang i intervallet. */
function useCoalescedRefetch(queryClient: QueryClient) {
  const timer = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, []);

  return useCallback(() => {
    if (timer.current) return; // En henting er allerede planlagt.
    timer.current = setTimeout(() => {
      timer.current = null;
      void queryClient.invalidateQueries({ queryKey: ORDERS_QUERY_KEY });
    }, REFETCH_INTERVAL_MS);
  }, [queryClient]);
}

export function useOrders(initialOrders: Order[]) {
  const queryClient = useQueryClient();
  const scheduleRefetch = useCoalescedRefetch(queryClient);

  const query = useQuery({
    queryKey: ORDERS_QUERY_KEY,
    queryFn: fetchOrdersFromBff,
    initialData: initialOrders,
    refetchInterval: false,
    staleTime: 1000,
  });

  const onEvent = useCallback(
    (message: MessageEvent) => {
      const event = parseEvent(message);
      if (!event?.data) {
        // Ingen data å skrive inn, men noe skjedde. Be om lista for sikkerhets skyld.
        scheduleRefetch();
        return;
      }

      // Skjermen oppdaterer seg her, uten nettverkskall. Selv om det kommer
      // femti hendelser i sekundet, er dette bare femti små cache-skrivinger.
      queryClient.setQueryData<Order[]>(ORDERS_QUERY_KEY, (orders) => applyEvent(orders, event));

      // …og sikkerhetsnettet, maks én gang i sekundet.
      scheduleRefetch();
    },
    [queryClient, scheduleRefetch],
  );

  const connection = useEventSource("/api/events", onEvent);

  return { ...query, connection };
}

/**
 * Baristaknappene, med optimistisk oppdatering.
 *
 * Vi flytter bestillingen i cachen med én gang, før serveren har svart. Går
 * kallet galt, ruller vi tilbake til det vi hadde. Knappen føles umiddelbar selv
 * om API-et eller broker er treg.
 */
export function useUpdateStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ orderId, status }: { orderId: string; status: OrderStatus }) => {
      const response = await fetch(`/api/orders/${orderId}`, {
        method: "PATCH",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ status }),
      });
      if (!response.ok) throw new Error("Klarte ikke å oppdatere status");
      return (await response.json()) as Order;
    },

    onMutate: async ({ orderId, status }) => {
      // Stopp hentinger som er på vei inn, ellers kan de overskrive det vi gjør her.
      await queryClient.cancelQueries({ queryKey: ORDERS_QUERY_KEY });
      const previous = queryClient.getQueryData<Order[]>(ORDERS_QUERY_KEY);

      queryClient.setQueryData<Order[]>(ORDERS_QUERY_KEY, (orders) =>
        (orders ?? []).map((order) =>
          order.orderId === orderId
            ? { ...order, status, updatedAt: new Date().toISOString() }
            : order,
        ),
      );

      return { previous };
    },

    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(ORDERS_QUERY_KEY, context.previous);
      }
    },

    // Uansett utfall: hent fasiten fra serveren til slutt.
    onSettled: () => queryClient.invalidateQueries({ queryKey: ORDERS_QUERY_KEY }),
  });
}
