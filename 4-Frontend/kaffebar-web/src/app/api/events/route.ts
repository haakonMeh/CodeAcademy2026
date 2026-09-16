import type { NextRequest } from "next/server";
import { subscribeToOrders, type Subscription } from "@/lib/rabbitmq";
import type { KaffebarEvent } from "@/types/domain";

export const dynamic = "force-dynamic";
export const runtime = "nodejs"; // amqplib bruker sockets — dette kan ikke kjøre på edge

/**
 * BFF-en. UTLEVERT — men dette er fila vi går gjennom sammen til slutt, så les
 * den mens du venter på at noe skal kompilere.
 *
 * Her er hele poenget med samlingen på tjue linjer: serversiden i
 * metarammeverket ditt *er* BFF-en din. Ruta abonnerer på RabbitMQ og relayer
 * videre som en tekststrøm. Nettleseren ser aldri en AMQP-pakke, trenger ingen
 * credentials og ingen klientbibliotek — `EventSource` ligger innebygd i den.
 *
 * Alt som er lett å gå seg vill i ligger ferdig og riktig her, fordi ingen av
 * delene lærer bort noe og hver av dem koster ti minutter i et rom:
 *
 *   - `dynamic = "force-dynamic"`: uten den kan ruta bli cachet, og strømmen tom.
 *   - `content-type: text/event-stream`: er den feil, venter nettleseren evig.
 *   - `x-accel-buffering: no` og `no-transform`: ellers kan en proxy holde på
 *     hendelsene til svaret er ferdig — og et SSE-svar blir aldri ferdig.
 *   - Heartbeat: en kommentar-linje med jevne mellomrom holder forbindelsen åpen
 *     gjennom lastbalanserere som kutter en stille strøm.
 *   - Opprydding på `request.signal`: uten den blir det liggende igjen en kø på
 *     broker for hver fane noen har åpnet.
 *   - Dør abonnementet, lukker vi strømmen. Da kobler nettleseren seg opp igjen av
 *     seg selv og får et nytt abonnement. Uten dette blir strømmen stående åpen og
 *     stille etter at broker har vært nede, og skjermen sier «live» uten å være det.
 *
 * Det du skriver i runde 2 er abonnementet under: `src/lib/rabbitmq.ts`.
 */

/** Hvor ofte vi sender en kommentar-linje for å holde forbindelsen i live. */
const HEARTBEAT_MS = 25_000;

export async function GET(request: NextRequest) {
  const encoder = new TextEncoder();

  const stream = new ReadableStream({
    async start(controller) {
      let closed = false;
      let subscription: Subscription | null = null;
      let heartbeat: NodeJS.Timeout | null = null;

      const send = (payload: string) => {
        if (closed) return;
        try {
          controller.enqueue(encoder.encode(payload));
        } catch {
          closed = true;
        }
      };

      /** Én SSE-melding. De to linjeskiftene til slutt er ikke valgfrie. */
      const sendEvent = (event: KaffebarEvent) => {
        send(`event: ${event.type}\ndata: ${JSON.stringify(event)}\n\n`);
      };

      const cleanup = async () => {
        if (closed) return;
        closed = true;
        if (heartbeat) clearInterval(heartbeat);
        await subscription?.close();
        try {
          controller.close();
        } catch {
          // Allerede lukket.
        }
      };

      // Lukker brukeren fanen, må vi kvitte oss med køen på broker.
      request.signal.addEventListener("abort", () => void cleanup());

      try {
        subscription = await subscribeToOrders(sendEvent, () => {
          // Abonnementet er dødt. Vi lukker strømmen i stedet for å bli stående og
          // si «live» uten å ha noe å sende: nettleseren kobler seg opp igjen av
          // seg selv, og da lages et nytt abonnement.
          send(
            `event: error\ndata: ${JSON.stringify({ message: "Mistet forbindelsen til RabbitMQ" })}\n\n`,
          );
          void cleanup();
        });
      } catch (error) {
        // RabbitMQ er nede. Da sier vi fra i stedet for å henge: klienten viser
        // «frakoblet» og lista faller tilbake på vanlig henting.
        const message = error instanceof Error ? error.message : String(error);
        console.warn("[kaffebar-web] Fikk ikke kontakt med RabbitMQ:", message);
        send(`event: error\ndata: ${JSON.stringify({ message })}\n\n`);
        await cleanup();
        return;
      }

      heartbeat = setInterval(() => send(": heartbeat\n\n"), HEARTBEAT_MS);

      // Første melding: si fra at vi er koblet på, så UI-et kan vise «live».
      send(`event: ready\ndata: ${JSON.stringify({ connectedAt: new Date().toISOString() })}\n\n`);
    },
  });

  return new Response(stream, {
    headers: {
      "content-type": "text/event-stream",
      "cache-control": "no-cache, no-transform",
      connection: "keep-alive",
      // Slår av bufring i nginx, som ellers holder på hendelsene til svaret er ferdig.
      "x-accel-buffering": "no",
    },
  });
}
