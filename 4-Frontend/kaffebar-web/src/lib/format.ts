/** Små formateringshjelpere. Utlevert. */

export function formatPrice(nok: number): string {
  return new Intl.NumberFormat("nb-NO", {
    style: "currency",
    currency: "NOK",
    maximumFractionDigits: 0,
  }).format(nok);
}

/** «nå», «4 min siden», «2 t siden» — nb-NO, uten ekstra pakker. */
export function timeAgo(iso: string, now: number = Date.now()): string {
  const seconds = Math.round((now - Date.parse(iso)) / 1000);
  if (!Number.isFinite(seconds)) return "";
  if (seconds < 45) return "nå";

  const formatter = new Intl.RelativeTimeFormat("nb-NO", { numeric: "auto" });
  if (seconds < 3600) return formatter.format(-Math.round(seconds / 60), "minute");
  if (seconds < 86_400) return formatter.format(-Math.round(seconds / 3600), "hour");
  return formatter.format(-Math.round(seconds / 86_400), "day");
}
