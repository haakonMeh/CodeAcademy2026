import type { NextRequest } from "next/server";
import { oldestMockWithStatus, setMockStatus } from "@/mocks/store";
import type { KaffebarEvent } from "@/types/domain";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

/**
 * RUNDE 1 — den falske hendelseskilden.  UTLEVERT, du skal ikke røre denne.
 *
 * Den gjør nøyaktig det `/api/events` skal gjøre senere i dag — sender en
 * Server-Sent Event hvert par sekund — men uten RabbitMQ, uten amqplib og uten
 * en eneste container. Hele poenget er at klientsiden din skal virke *før* vi
 * introduserer infrastruktur, slik at du vet at det er rørene som er feil hvis
 * noe ryker i runde 2, ikke koden du nettopp skrev.
 *
 * Den flytter en ordre ett steg videre i det samme minne-lageret mock-API-et
 * bruker (`USE_MOCK_API=true`), slik at lista faktisk endrer seg og ikke bare
 * blinker. Når alt er READY, begynner den forfra.
 */

const TICK_MS = 2_000;

/** Rekkefølgen vi prøver i: klar først, så ny — og til slutt tilbake til start. */
const TRANSITIONS = [
  ["BREWING", "READY"],
  ["PENDING", "BREWING"],
  ["READY", "PENDING"],
] as const;

/** Flytter én ordre ett steg videre, og begynner forfra når alt er klart. */
function advanceOneOrder(): KaffebarEvent | undefined {
  for (const [from, to] of TRANSITIONS) {
    const candidate = oldestMockWithStatus(from);
    if (!candidate) continue;

    const result = setMockStatus(candidate.orderId, to);
    if (!result) continue;

    return {
      type: "order.status-changed",
      timestamp: new Date().toISOString(),
      data: result.order,
      previousStatus: result.previousStatus,
    };
  }

  return undefined;
}

export async function GET(request: NextRequest) {
  const encoder = new TextEncoder();

  const stream = new ReadableStream({
    start(controller) {
      let closed = false;

      const send = (payload: string) => {
        if (closed) return;
        try {
          controller.enqueue(encoder.encode(payload));
        } catch {
          closed = true;
        }
      };

      const tick = setInterval(() => {
        const event = advanceOneOrder();
        if (event) send(`event: ${event.type}\ndata: ${JSON.stringify(event)}\n\n`);
      }, TICK_MS);

      request.signal.addEventListener("abort", () => {
        closed = true;
        clearInterval(tick);
        try {
          controller.close();
        } catch {
          // Allerede lukket.
        }
      });

      // Første melding: si fra at vi er koblet på, så LiveIndicator slår om til «live».
      send(`event: ready\ndata: ${JSON.stringify({ connectedAt: new Date().toISOString(), demo: true })}\n\n`);
    },
  });

  return new Response(stream, {
    headers: {
      "content-type": "text/event-stream",
      "cache-control": "no-cache, no-transform",
      connection: "keep-alive",
      "x-accel-buffering": "no",
    },
  });
}
