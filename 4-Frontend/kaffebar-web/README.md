# kaffebar-web

Frontenden for Kaffebar — skjelettet du jobber i under workshopen i samling 4.

Oppgaveteksten ligger i [`../oppgave.md`](../oppgave.md). Start der.

## Kom i gang

**Runde 1 — uten containere:**

```bash
cd 4-Frontend/kaffebar-web
corepack enable
yarn install
USE_MOCK_API=true yarn dev
```

**Runde 2 — med ekte API og broker.** Fra rota av repoet, i et eget vindu:

```bash
docker compose --profile java up --build      # eller --profile dotnet
```

…og så `yarn dev` (uten `USE_MOCK_API`) her.

http://localhost:3000 — kundeskjerm på `/`, baristaskjerm på `/barista`.

Skjelettet **kjører fra første `yarn dev`** og viser data. Det som står stille er den
live oppdateringen. Det er meningen.

## Hva som er utlevert, og hva du skriver

Alt av oppsett, styling, datahenting og presentasjonskomponenter er ferdig. Ingen skal
bruke workshoptid på CSS eller på `fetch`.

| Fil | Status | Når |
| --- | --- | --- |
| `src/hooks/useEventSource.ts` | **`// TODO`** — hooken gjør ingenting | Runde 1 |
| `src/lib/rabbitmq.ts` | **`// TODO`** — tre kall mangler | Runde 2 |
| `src/hooks/useOrders.ts` | Utlevert — men du bytter `EVENTS_URL` her | Runde 2 |
| `Dockerfile` | **`// TODO`** — tom | Ekstraoppgave 7 |
| `src/app/api/events/demo/route.ts` | Utlevert — den falske kilden | — |
| `src/app/api/events/route.ts` | Utlevert — SSE-en, med alle fallgruvene ryddet unna | — |
| `src/lib/api.ts` | Utlevert — HTTP mot Kaffebar-API-et | — |
| `src/app/api/orders/**` | Utlevert — BFF-rutene | — |
| `src/components/**` | Utlevert — alle presentasjonskomponenter | — |
| `src/types/kaffebar.ts` | Generert fra kontrakten med `yarn generate:types` | — |

### Fasit

Repoet bruker `-solution`-suffiks for fasit (samme konvensjon som
`1-DevOps/2-Docker/Dockerfile-solution`). Ingen løsningsbrancher.

| Fasit | Erstatter | Gjelder |
| --- | --- | --- |
| `src/hooks/useEventSource-solution.ts` | `src/hooks/useEventSource.ts` | Oppgave 1 |
| `src/lib/rabbitmq-solution.ts` | `src/lib/rabbitmq.ts` | Oppgave 2 |
| `src/hooks/useOrders-solution.ts` | `src/hooks/useOrders.ts` | Ekstraoppgave 3 og 4 |
| `Dockerfile-solution` | `Dockerfile` | Ekstraoppgave 7 |

Kopier innholdet inn i fila den erstatter. Rekker du ikke en oppgave, gjør det og gå
videre.

Blir du ferdig før tiden, ligger det ni ekstraoppgaver i `../oppgave.md`. De fire første
trenger hverken nye containere eller kode fra mai.

## Arkitektur

```
                          ┌─ HTTP (typer generert fra kontrakten) ──> kaffebar-api
nettleser ──> kaffebar-web ─┤
                          └─ SSE <── route handler <── amqplib <── RabbitMQ
```

Nettleseren snakker **bare** med Next-appen. Next-appen er sin egen BFF: den henter data
fra Kaffebar-API-et på serversiden, og den abonnerer på RabbitMQ i en route handler og
relayer videre som Server-Sent Events. Derfor er `KAFFEBAR_API_URL` ikke prefikset med
`NEXT_PUBLIC_` — den skal aldri ut i nettleseren.

## Miljøvariabler

| Variabel | Default | Hva |
| --- | --- | --- |
| `KAFFEBAR_API_URL` | `http://localhost:8080` | Hvilket Kaffebar-API vi snakker med |
| `RABBITMQ_URL` | `amqp://guest:guest@localhost:5672` | Broker, brukt av `/api/events` |
| `RABBITMQ_EXCHANGE` | `kaffebar` | Exchangen vi abonnerer på |
| `USE_MOCK_API` | `false` | Kjør uten containere |

### Pek på ditt eget API fra samling 3

Fullførte du ditt eget Kaffebar-API i mai, kan du bruke det i stedet for fasiten:

```bash
KAFFEBAR_API_URL=http://localhost:5035 yarn dev          # .NET-sporet
KAFFEBAR_API_URL=http://localhost:8080 yarn dev          # Java-sporet
KAFFEBAR_API_URL=http://localhost:5035 yarn generate:types
```

Det er ekstraoppgave 8, og den beste testen på om kontrakten din faktisk holder.

## Mock-modus (uten Docker)

```bash
USE_MOCK_API=true yarn dev
```

Appen svarer da fra et lager i minnet i stedet for fra `kaffebar-api`: samme meny, samme
statusflyt, og en auto-barista som flytter bestillinger hvert åttende sekund. Runde 1
fungerer i sin helhet uten en eneste container; runde 2 trenger en broker.

Koden ligger i `src/mocks/`. `src/lib/api.ts` går rett dit i mock-modus, og
`instrumentation.ts` starter auto-baristaen. Det er ingen nettverkskall involvert, og
derfor heller ingenting som kan feile halvveis.

## Godt å vite i utviklingsmodus

- **React kjører effekter to ganger** (StrictMode). To SSE-tilkoblinger i nettverksfanen
  lokalt er forventet, ikke en feil. Det skjer ikke i produksjonsbygget.
- **Hot reload og AMQP.** Hver lagring laster serverkoden på nytt. Derfor ligger
  AMQP-tilkoblingen på `globalThis` — uten den vaktposten hoper duplikate consumere seg
  opp, og hendelsene kommer flere ganger. Ser du det likevel: restart `yarn dev`.

## Kommandoer

```bash
yarn dev              # utviklingsserver på :3000
yarn build            # produksjonsbygg (standalone)
yarn test             # Vitest + Testing Library
yarn lint
yarn generate:types   # openapi-typescript mot KAFFEBAR_API_URL
```

## Stack

Next 16 (App Router), React 19, TypeScript `strict`, Tailwind 4, TanStack Query,
Vitest + Testing Library, MSW, yarn. Anbefalingene bak valgene står i
[`../BEST-PRACTICE.md`](../BEST-PRACTICE.md).
