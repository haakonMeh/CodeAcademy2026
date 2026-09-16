import { ApiUnavailable } from "@/components/ApiUnavailable";
import { BaristaBoard } from "@/components/BaristaBoard";
import { fetchOrders } from "@/lib/api";
import { KAFFEBAR_API_URL } from "@/lib/config";
import type { Order } from "@/types/domain";

/**
 * Baristaskjermen — en server component som henter den første tilstanden, og
 * overlater resten til klienten.
 *
 * Åpne denne i ett vindu og kundeskjermen i et annet. Når hooken din er på
 * plass, flytter en ordre seg mellom kolonnene her uten at du refresher.
 */
export const dynamic = "force-dynamic";

type PageData = { ok: true; orders: Order[] } | { ok: false; message: string };

async function load(): Promise<PageData> {
  try {
    return { ok: true, orders: await fetchOrders({ limit: 100 }) };
  } catch (error) {
    return { ok: false, message: error instanceof Error ? error.message : "Ukjent feil" };
  }
}

export default async function BaristaPage() {
  const data = await load();

  return (
    <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Baristaskjerm</h1>
        <p className="mt-1 text-secondary">
          Køen, gruppert på status. Oppdaterer seg selv når det kommer en hendelse.
        </p>
      </header>

      {data.ok ? (
        <BaristaBoard initialOrders={data.orders} />
      ) : (
        <ApiUnavailable url={KAFFEBAR_API_URL} message={data.message} />
      )}
    </main>
  );
}
