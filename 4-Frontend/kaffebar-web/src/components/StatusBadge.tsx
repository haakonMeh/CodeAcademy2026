import { STATUS_LABEL, type OrderStatus } from "@/types/domain";

const STYLES: Record<OrderStatus, string> = {
  PENDING: "bg-[var(--status-pending-soft)] text-[var(--status-pending)]",
  BREWING: "bg-[var(--status-brewing-soft)] text-[var(--status-brewing)]",
  READY: "bg-[var(--status-ready-soft)] text-[var(--status-ready)]",
};

export function StatusBadge({ status }: { status: OrderStatus }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold ${STYLES[status]}`}
    >
      <span aria-hidden className="h-1.5 w-1.5 rounded-full bg-current" />
      {STATUS_LABEL[status]}
    </span>
  );
}
