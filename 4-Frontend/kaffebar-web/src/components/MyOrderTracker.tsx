"use client";

import { OrderCard } from "./OrderCard";
import { LiveIndicator } from "./LiveIndicator";
import { useOrders } from "@/hooks/useOrders";
import type { Order } from "@/types/domain";

/**
 * «Din bestilling» på kundeskjermen. Utlevert.
 *
 * Samme hook som baristaskjermen bruker — så når baristaen trykker «Marker som
 * klar» i det andre vinduet, endrer dette kortet seg uten at kunden gjør noe.
 * Det er den demoen.
 */
export function MyOrderTracker({
  orderId,
  initialOrders,
}: {
  orderId: string | null;
  initialOrders: Order[];
}) {
  const { data: orders = [], connection } = useOrders(initialOrders);
  const order = orderId ? orders.find((candidate) => candidate.orderId === orderId) : undefined;

  if (!order) return null;

  return (
    <section className="flex flex-col gap-3 rounded-2xl border border-line bg-accent-soft/60 p-4">
      <div className="flex items-center justify-between gap-3">
        <h2 className="font-semibold">Din bestilling</h2>
        <LiveIndicator state={connection} />
      </div>
      <OrderCard order={order} showBadge />
    </section>
  );
}
