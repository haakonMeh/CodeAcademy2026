import amqp, { type ChannelModel } from "amqplib";
import { RABBITMQ_EXCHANGE, RABBITMQ_URL, USE_MOCK_API } from "./config";
import type { KaffebarEvent } from "@/types/domain";

/**
 * FASIT for runde 2. Kopier innholdet inn i `rabbitmq.ts` hvis du ikke rakk det.
 *
 * assertExchange → assertQueue → bindQueue → consume. Fire kall, samme fire som
 * i april — forskjellen er at hver lytter her får sin egen eksklusive kø, slik
 * at alle som ser på skjermen får alle hendelsene.
 */

export interface Subscription {
  close: () => Promise<void>;
}

export type EventHandler = (event: KaffebarEvent) => void;

const globalForAmqp = globalThis as typeof globalThis & {
  __kaffebarConnection?: Promise<ChannelModel>;
};

function getConnection(): Promise<ChannelModel> {
  globalForAmqp.__kaffebarConnection ??= amqp
    .connect(RABBITMQ_URL)
    .then((connection) => {
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

export async function subscribeToOrders(
  onEvent: EventHandler,
  onClosed?: () => void,
): Promise<Subscription> {
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

  // 1. Exchangen finnes allerede — API-et deklarerte den da det startet. Vi
  //    deklarerer den likevel: assertExchange er idempotent, og da spiller det
  //    ingen rolle hvem som starter først.
  await channel.assertExchange(RABBITMQ_EXCHANGE, "topic", { durable: true });

  // 2. Egen kø per lytter, borte når vi kobler fra.
  const queue = await channel.assertQueue("", { exclusive: true, autoDelete: true });
  await channel.bindQueue(queue.queue, RABBITMQ_EXCHANGE, "order.#");

  // 3. noAck: et UI som mister en hendelse skal ikke blokkere køen — neste
  //    hendelse henter uansett hele lista på nytt.
  const consumer = await channel.consume(
    queue.queue,
    (message) => {
      if (!message) return;
      try {
        onEvent(JSON.parse(message.content.toString()) as KaffebarEvent);
      } catch (error) {
        console.error("Klarte ikke å tolke hendelse fra RabbitMQ:", error);
      }
    },
    { noAck: true },
  );

  return {
    close: async () => {
      try {
        await channel.cancel(consumer.consumerTag);
        await channel.close();
      } catch {
        // Kanalen kan allerede være borte.
      }
    },
  };
}
