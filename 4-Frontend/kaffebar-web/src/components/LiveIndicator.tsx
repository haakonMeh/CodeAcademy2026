"use client";

import type { ConnectionState } from "@/hooks/useEventSource";

const LABEL: Record<ConnectionState, string> = {
  open: "Live",
  connecting: "Kobler til …",
  closed: "Frakoblet",
};

const DOT: Record<ConnectionState, string> = {
  open: "bg-[var(--status-ready)] animate-pulse-soft",
  connecting: "bg-[var(--status-pending)] animate-pulse-soft",
  closed: "bg-muted",
};

/**
 * Sier om SSE-forbindelsen faktisk står. Står den ikke, er det verdt å vite —
 * en kø som ser riktig ut men står stille er verre enn en som sier fra.
 */
export function LiveIndicator({ state }: { state: ConnectionState }) {
  return (
    <span
      className="inline-flex items-center gap-2 rounded-full border border-line bg-card px-3 py-1 text-xs font-medium text-secondary"
      title={
        state === "closed"
          ? "Ingen hendelser. Kjører RabbitMQ? Er /api/events implementert?"
          : undefined
      }
    >
      <span aria-hidden className={`h-2 w-2 rounded-full ${DOT[state]}`} />
      {LABEL[state]}
    </span>
  );
}
