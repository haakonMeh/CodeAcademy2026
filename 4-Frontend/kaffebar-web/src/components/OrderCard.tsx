"use client";

import { StatusBadge } from "./StatusBadge";
import { timeAgo } from "@/lib/format";
import { MILK_LABEL, SIZE_LABEL, type Order, type OrderStatus } from "@/types/domain";

/** Neste steg i flyten, eller null hvis ordren er ferdig. */
export function nextStatus(status: OrderStatus): OrderStatus | null {
  if (status === "PENDING") return "BREWING";
  if (status === "BREWING") return "READY";
  return null;
}

const NEXT_LABEL: Record<string, string> = {
  BREWING: "Start brygging",
  READY: "Marker som klar",
};

export function OrderCard({
  order,
  onAdvance,
  busy = false,
  showBadge = false,
}: {
  order: Order;
  onAdvance?: (order: Order, status: OrderStatus) => void;
  busy?: boolean;
  showBadge?: boolean;
}) {
  const next = nextStatus(order.status);
  const details = [
    SIZE_LABEL[order.size],
    order.milkType ? MILK_LABEL[order.milkType] : null,
    order.extraShot ? "ekstra shot" : null,
    order.quantity > 1 ? `${order.quantity} stk` : null,
  ].filter(Boolean);

  return (
    <article className="animate-fade-in rounded-xl border border-line bg-card p-4 shadow-sm">
      <header className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-semibold">{order.customerName}</p>
          <p className="truncate text-sm text-secondary">{order.coffeeName}</p>
        </div>
        <time className="shrink-0 text-xs text-muted" dateTime={order.createdAt}>
          {timeAgo(order.createdAt)}
        </time>
      </header>

      {details.length > 0 && (
        <p className="mt-2 text-xs text-muted">{details.join(" · ")}</p>
      )}

      {showBadge && (
        <div className="mt-3">
          <StatusBadge status={order.status} />
        </div>
      )}

      {onAdvance && next && (
        <button
          type="button"
          disabled={busy}
          onClick={() => onAdvance(order, next)}
          className="mt-3 w-full rounded-lg bg-accent px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-accent-strong disabled:cursor-not-allowed disabled:opacity-50"
        >
          {busy ? "Oppdaterer …" : NEXT_LABEL[next]}
        </button>
      )}
    </article>
  );
}
