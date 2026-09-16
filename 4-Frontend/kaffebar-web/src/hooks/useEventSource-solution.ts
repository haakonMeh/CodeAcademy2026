"use client";

import { useEffect, useRef, useState } from "react";

/**
 * FASIT for oppgave 1 (runde 1). Kopier innholdet inn i `useEventSource.ts`.
 */

export type ConnectionState = "connecting" | "open" | "closed";

/** Hendelsesnavnene route handleren sender. `message` dekker de uten navn. */
const EVENT_NAMES = ["order.created", "order.status-changed", "message"] as const;

export function useEventSource(
  url: string,
  onEvent: (event: MessageEvent) => void,
): ConnectionState {
  const [state, setState] = useState<ConnectionState>("connecting");

  // `onEvent` er en ny funksjon for hver render. Legger vi den i
  // avhengighetslista under, river vi ned forbindelsen og bygger den opp igjen
  // på hver eneste render. Vi holder den i en ref i stedet — da kan effekten
  // være avhengig av `url` alene.
  const handlerRef = useRef(onEvent);
  useEffect(() => {
    handlerRef.current = onEvent;
  }, [onEvent]);

  useEffect(() => {
    const source = new EventSource(url);
    const listener = (event: MessageEvent) => handlerRef.current(event);

    source.addEventListener("open", () => setState("open"));
    source.addEventListener("ready", () => setState("open"));

    for (const name of EVENT_NAMES) {
      source.addEventListener(name, listener);
    }

    // Serveren sier fra hvis den ikke fikk kontakt med RabbitMQ. Da er det ikke
    // vits i at nettleseren prøver å koble opp igjen i det uendelige.
    source.addEventListener("error", () => {
      setState(source.readyState === EventSource.CLOSED ? "closed" : "connecting");
    });

    return () => {
      source.close();
      setState("closed");
    };
  }, [url]);

  return state;
}
