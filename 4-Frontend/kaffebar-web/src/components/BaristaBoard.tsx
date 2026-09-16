"use client";

import { useState } from "react";
import { StatusColumn } from "./StatusColumn";
import { LiveIndicator } from "./LiveIndicator";
import { useOrders, useUpdateStatus } from "@/hooks/useOrders";
import { STATUS_ORDER, type Order, type OrderStatus } from "@/types/domain";

/**
 * Baristaskjermen. Utlevert.
 *
 * Denne komponenten er grunnen til at hendelsene er verdt bryet: den har ingen egen
 * tilstand for ordrene. Den leser fra TanStack Query, og `useOrders` invaliderer
 * cachen når det kommer en hendelse over SSE. Ingen polling, ingen manuell synk.
 */
export function BaristaBoard({ initialOrders }: { initialOrders: Order[] }) {
  const { data: orders = [], connection } = useOrders(initialOrders);
  const updateStatus = useUpdateStatus();
  const [busyOrderId, setBusyOrderId] = useState<string | null>(null);

  const advance = (order: Order, status: OrderStatus) => {
    setBusyOrderId(order.orderId);
    updateStatus.mutate(
      { orderId: order.orderId, status },
      { onSettled: () => setBusyOrderId(null) },
    );
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-secondary">
          {orders.length} bestilling{orders.length === 1 ? "" : "er"} i køen
        </p>
        <LiveIndicator state={connection} />
      </div>

      {connection === "closed" && (
        <p className="rounded-lg border border-line bg-accent-soft px-3 py-2 text-sm text-secondary">
          Ingen live-forbindelse. Køen oppdaterer seg ikke av seg selv — sjekk hooken
          i <code className="font-mono">src/hooks/useEventSource.ts</code> (oppgave 1),
          og at kilden i <code className="font-mono">useOrders.ts</code> peker riktig.
        </p>
      )}

      <div className="grid gap-6 md:grid-cols-3">
        {STATUS_ORDER.map((status) => (
          <StatusColumn
            key={status}
            status={status}
            orders={orders.filter((order) => order.status === status)}
            onAdvance={advance}
            busyOrderId={busyOrderId}
          />
        ))}
      </div>
    </div>
  );
}
