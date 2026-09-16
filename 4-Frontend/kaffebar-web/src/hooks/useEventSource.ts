"use client";

import { useEffect, useState } from "react";

/**
 * RUNDE 1 — her begynner du.  Se 4-Frontend/oppgave.md, oppgave 1.
 *
 * Hooken som kobler seg på en hendelsesstrøm og kaller `onEvent` hver gang det
 * kommer noe. Den peker foreløpig på `/api/events/demo` — en falsk kilde som
 * ligger ferdig i repoet og sender en hendelse annethvert sekund uten RabbitMQ,
 * uten amqplib og uten en eneste container. Får du denne til å virke, har du
 * hele kjeden fra hendelse til piksel på plass. Runde 2 bytter bare ut kilden.
 *
 * Skjelettet gjør ingenting, så skjermen står stille — den krasjer ikke, den
 * bare oppdaterer seg ikke. Fasit: `useEventSource-solution.ts`.
 *
 * Verktøyet du trenger — ingen pakke, alt ligger i nettleseren:
 *   - `new EventSource(url)`
 *   - `source.addEventListener("order.status-changed", handler)` for navngitte
 *     hendelser. Serveren sender også en `ready` når forbindelsen er oppe, og
 *     en `order.created` når noen bestiller.
 *   - `source.close()` i opprydningsfunksjonen fra useEffect.
 *
 * To ting det er lett å gå i:
 *   - `onEvent` er en ny funksjon for hver render. Har du den i avhengighets-
 *     lista til useEffect, river du ned forbindelsen og bygger den opp igjen på
 *     hver eneste render. Bruk en ref. Dette er den klassiske React-fella.
 *   - React kjører effekter to ganger i utviklingsmodus (StrictMode), med vilje.
 *     Ser du to tilkoblinger i nettverksfanen lokalt, er det forventet — ikke en
 *     feil du skal bruke workshoptid på. I produksjonsbygget skjer det ikke.
 */

export type ConnectionState = "connecting" | "open" | "closed";

export function useEventSource(
  url: string,
  onEvent: (event: MessageEvent) => void,
): ConnectionState {
  const [state] = useState<ConnectionState>("closed");

  useEffect(() => {
    // TODO (runde 1): opprett en EventSource mot `url`, kall `onEvent` for hver
    //                 hendelse, og lukk forbindelsen når komponenten forsvinner.
    //                 Sett `state` til "open" når forbindelsen er oppe, slik at
    //                 indikatoren øverst til høyre slår om til «live».
    void url;
    void onEvent;
  }, [url, onEvent]);

  return state;
}
