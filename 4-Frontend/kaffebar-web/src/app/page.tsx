import { ApiUnavailable } from "@/components/ApiUnavailable";
import { CustomerScreen } from "@/components/CustomerScreen";
import { fetchMenu, fetchOrders } from "@/lib/api";
import { KAFFEBAR_API_URL } from "@/lib/config";
import type { Coffee, Order } from "@/types/domain";

/**
 * Kundeskjermen — en server component.
 *
 * Datahentingen skjer på serveren, før HTML-en sendes: ingen loading-spinner,
 * ingen useEffect, ingen API-adresse i nettleseren. Hentingen er utlevert
 * (src/lib/api.ts); det du skriver i dag er hendelsene som holder skjermen fersk.
 */
export const dynamic = "force-dynamic";

type PageData =
  | { ok: true; menu: Coffee[]; orders: Order[] }
  | { ok: false; message: string };

/** En container som ikke er oppe skal gi en beskjed du kan handle på. */
async function load(): Promise<PageData> {
  try {
    const [menu, orders] = await Promise.all([fetchMenu(), fetchOrders({ limit: 50 })]);
    return { ok: true, menu, orders };
  } catch (error) {
    return { ok: false, message: error instanceof Error ? error.message : "Ukjent feil" };
  }
}

export default async function CustomerPage() {
  const data = await load();

  return (
    <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <header className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Bestill kaffe</h1>
        <p className="mt-1 text-secondary">
          Velg en kaffe, skriv navnet ditt, og følg bestillingen mens baristaen jobber.
        </p>
      </header>

      {data.ok ? (
        <CustomerScreen menu={data.menu} initialOrders={data.orders} />
      ) : (
        <ApiUnavailable url={KAFFEBAR_API_URL} message={data.message} />
      )}
    </main>
  );
}
