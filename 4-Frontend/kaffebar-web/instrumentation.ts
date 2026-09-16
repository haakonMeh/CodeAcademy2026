/**
 * Kjøres én gang når Next-serveren starter.
 *
 * Eneste jobb: starte mock-baristaen hvis USE_MOCK_API=true, slik at
 * bestillinger flytter seg av seg selv også uten containere. Selve
 * datahentingen i mock-modus går rett i minnelageret, se `src/mocks/api.ts`.
 * Er mock-modus av, gjør denne fila ingenting.
 */
export async function register() {
  if (process.env.NEXT_RUNTIME !== "nodejs") return;
  if (process.env.USE_MOCK_API !== "true") return;

  const { startMockBarista } = await import("@/mocks/events");
  startMockBarista();

  console.log("[kaffebar-web] Mock-modus er PÅ — ingen containere brukes.");
}
