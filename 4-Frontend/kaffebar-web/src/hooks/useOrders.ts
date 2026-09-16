"use client";

import { useCallback } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEventSource } from "./useEventSource";
import type { Order, OrderStatus } from "@/types/domain";

/**
 * Ordrelista, holdt fersk.
 *
 * Utlevert — men les den. Den viser hvorfor hendelsesbiten du skriver i dag er
 * verdt bryet: når det kommer en hendelse, sier vi bare fra til TanStack Query
 * at cachen er gammel. Query henter på nytt og oppdaterer alle komponenter som
 * bruker den. Vi rører aldri cachen manuelt, og vi har ingen egen tilstand å
 * holde i synk.
 *
 * Det finnes et alternativ: skrive hendelsens `data`-objekt rett inn i cachen
 * med `setQueryData`. Det er raskere, men da må du selv håndtere hendelser som
 * kommer i feil rekkefølge. Invalidering er den kjedelige og riktige
 * standardløsningen — start der.
 */

export const ORDERS_QUERY_KEY = ["orders"] as const;

/**
 * Hendelseskilden. Dette er den ene linja du bytter mellom runde 1 og runde 2:
 *
 *   Runde 1:  "/api/events/demo"   falsk kilde, ingen containere
 *   Runde 2:  "/api/events"        ekte hendelser fra RabbitMQ, via src/lib/rabbitmq.ts
 *
 * At det er én linje er ikke tilfeldig: for resten av appen er en hendelse en
 * hendelse. Hvor den kom fra er BFF-ens problem, ikke UI-ets.
 */
const EVENTS_URL = "/api/events/demo";

async function fetchOrdersFromBff(): Promise<Order[]> {
  const response = await fetch("/api/orders", { cache: "no-store" });
  if (!response.ok) throw new Error("Klarte ikke å hente bestillinger");
  return response.json();
}

export function useOrders(initialOrders: Order[]) {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ORDERS_QUERY_KEY,
    queryFn: fetchOrdersFromBff,
    initialData: initialOrders,
    // Hendelsene forteller oss når noe har endret seg. Da trenger vi ikke polle.
    refetchInterval: false,
    staleTime: 1000,
  });

  const onEvent = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ORDERS_QUERY_KEY });
  }, [queryClient]);

  const connection = useEventSource(EVENTS_URL, onEvent);

  return { ...query, connection };
}

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
    // Vi henter på nytt med én gang i stedet for å vente på at hendelsen skal
    // komme tilbake via RabbitMQ. Da føles knappen umiddelbar selv om broker
    // er treg — eller nede.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ORDERS_QUERY_KEY }),
  });
}
