import amqp, { type ChannelModel } from "amqplib";
import { RABBITMQ_EXCHANGE, RABBITMQ_URL, USE_MOCK_API } from "./config";
import type { KaffebarEvent } from "@/types/domain";

/**
 * RUNDE 2 — her fyller du inn.  Se 4-Frontend/oppgave.md, oppgave 2.
 *
 * Dette er det eneste stedet i appen som snakker AMQP. Alt rundt ligger ferdig:
 * tilkoblingen, kanalen, opprydningen, og route handleren som gjør hendelsene om
 * til Server-Sent Events. Det du skriver er de linjene som bærer poenget —
 * exchange, routing key og consume-callbacken.
 *
 * Det er nøyaktig de samme fire kallene som i consumeren du skrev i april:
 *
 *     assertExchange → assertQueue → bindQueue → consume
 *
 * Fasit: `rabbitmq-solution.ts` i samme mappe.
 *
 * Én forskjell fra april er verdt å merke seg: der konkurrerte consumerne om
 * meldingene i én delt kø. Her skal *alle* som ser på skjermen få *alle*
 * hendelsene, så hver lytter får sin egen eksklusive kø som slettes når fanen
 * lukkes. Det er forskjellen på en arbeidskø og en kringkasting.
 */

export interface Subscription {
  /** Lukker kanal og kø. Kall den alltid når klienten kobler fra. */
  close: () => Promise<void>;
}

export type EventHandler = (event: KaffebarEvent) => void;

/**
 * UTLEVERT — og verdt å lese.
 *
 * Tilkoblingen ligger på `globalThis`, ikke i en vanlig modulvariabel. Grunnen
 * er hot reload: hver gang du lagrer en fil, laster Next modulen på nytt. Uten
 * denne vaktposten ville du fått én ny AMQP-tilkobling per lagring, og etter
 * tjue minutters koding kom hver hendelse tjue ganger. Det ser ut som en bug i
 * koden din, men er en bug i utviklingsoppsettet. Samme mønster som
 * Prisma-singletonen de fleste har sett i en Next-app.
 */
const globalForAmqp = globalThis as typeof globalThis & {
  __kaffebarConnection?: Promise<ChannelModel>;
};

function getConnection(): Promise<ChannelModel> {
  globalForAmqp.__kaffebarConnection ??= amqp
    .connect(RABBITMQ_URL)
    .then((connection) => {
      // Dør forbindelsen, skal neste kall lage en ny — ikke gjenbruke en død.
      connection.on("close", () => void (globalForAmqp.__kaffebarConnection = undefined));
      connection.on("error", () => void (globalForAmqp.__kaffebarConnection = undefined));
      return connection;
    })
    .catch((error: unknown) => {
      globalForAmqp.__kaffebarConnection = undefined;
      throw error;
    });

  return globalForAmqp.__kaffebarConnection;
}

/**
 * Abonnerer på alle `order.*`-hendelser og kaller `onEvent` for hver.
 * Kaster hvis broker ikke er tilgjengelig — route handleren bestemmer hva som
 * skjer da (den sier fra til nettleseren i stedet for å henge).
 */
export async function subscribeToOrders(
  onEvent: EventHandler,
  onClosed?: () => void,
): Promise<Subscription> {
  // Mock-modus (USE_MOCK_API=true) har ingen broker. Da bruker vi den samme
  // falske kilden som i runde 1, slik at resten av appen er uvitende om forskjellen.
  if (USE_MOCK_API) {
    const { subscribeToMockOrders } = await import("@/mocks/events");
    return subscribeToMockOrders(onEvent);
  }

  const connection = await getConnection();
  const channel = await connection.createChannel();

  // Dør kanalen — fordi broker forsvant, eller fordi noen startet den på nytt — er
  // abonnementet vårt borte for godt. Da sier vi fra, slik at route handleren kan
  // lukke strømmen og la nettleseren koble seg opp igjen med et nytt abonnement.
  channel.on("error", () => {
    // «close» kommer rett etterpå. Lytteren må finnes, ellers kaster amqplib.
  });
  channel.on("close", () => onClosed?.());

  let consumerTag: string | undefined;

  // ---------------------------------------------------------------------------
  // TODO (runde 2). Fire kall, i denne rekkefølgen, alle på `channel`:
  //
  //   1. assertExchange   Exchangen heter RABBITMQ_EXCHANGE ("kaffebar") og er av
  //                       typen "topic". Den finnes allerede, for API-et laget den
  //                       da det startet, men assertExchange er idempotent, så det
  //                       spiller ingen rolle hvem som er først.
  //
  //   2. assertQueue      Tomt kønavn, så finner broker på et selv. Du vil ha
  //                       { exclusive: true, autoDelete: true }: køen er din alene
  //                       og forsvinner når du kobler fra. Alle som ser på skjermen
  //                       skal få alle hendelsene, så dette er en kringkasting, ikke
  //                       en arbeidskø.
  //
  //   3. bindQueue        Bind køen din til exchangen med en routing key som fanger
  //                       "order.created" og "order.status-changed". `order.*`
  //                       matcher ett ledd, `order.#` matcher null eller flere.
  //                       Punktum skiller leddene i AMQP, ikke bindestrek.
  //
  //   4. consume          Meldingen kan være null, så sjekk den først. Innholdet er
  //                       JSON i `message.content`, og skal tolkes og sendes videre
  //                       til `onEvent`. Bruk { noAck: true }: et UI som mister en
  //                       hendelse skal ikke blokkere køen, og neste hendelse henter
  //                       uansett hele lista på nytt.
  //                       Ta vare på `consumerTag` fra svaret, så opprydningen
  //                       nedenfor kan avslutte abonnementet.
  //
  // Står du fast: `rabbitmq-solution.ts` i samme mappe.
  // ---------------------------------------------------------------------------
  // Vakten under holder kompilatoren i ro, og sier samtidig fra så lenge oppgaven
  // ikke er løst. Uten den ville route handleren meldt «klar» til nettleseren selv
  // om ingen hendelser kunne komme, og indikatoren hadde stått på «Live» og løyet.
  // Den forsvinner av seg selv når du setter `consumerTag` i consume-kallet.
  void RABBITMQ_EXCHANGE;
  void onEvent;

  if (!consumerTag) {
    await channel.close();
    throw new Error(
      "Abonnementet i src/lib/rabbitmq.ts er ikke skrevet ferdig: consumerTag er ikke satt. " +
        "Se oppgave 2 i 4-Frontend/oppgave.md.",
    );
  }

  return {
    close: async () => {
      try {
        if (consumerTag) await channel.cancel(consumerTag);
        // Bare kanalen lukkes. Tilkoblingen er delt og lever videre.
        await channel.close();
      } catch {
        // Kanalen kan allerede være borte. Det er greit.
      }
    },
  };
}
