/**
 * Miljøvariabler, ett sted.
 *
 * NB: dette er serverside-konfigurasjon. Ingen av disse verdiene er prefikset
 * med NEXT_PUBLIC_, så de havner aldri i nettleseren — nettleseren snakker bare
 * med Next-appen, og Next-appen snakker med Kaffebar-API-et. Det er BFF-mønsteret.
 */

/** Kaffebar-API-et. Pek den på ditt eget API fra samling 3 hvis du vil (bonussteg 4). */
export const KAFFEBAR_API_URL = process.env.KAFFEBAR_API_URL ?? "http://localhost:8080";

/** RabbitMQ. Brukes bare av route handleren på /api/events. */
export const RABBITMQ_URL =
  process.env.RABBITMQ_URL ?? "amqp://guest:guest@localhost:5672";

export const RABBITMQ_EXCHANGE = process.env.RABBITMQ_EXCHANGE ?? "kaffebar";

/**
 * Nødløsning: kjør hele appen uten Docker. Se README.
 * Ikke standardvalget — containerne er poenget — men det redder deg hvis
 * Docker ikke vil i rommet.
 */
export const USE_MOCK_API = process.env.USE_MOCK_API === "true";
