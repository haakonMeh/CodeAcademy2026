# Workshop: Sett en frontend på miljøet

I mai bygde vi API-et. I dag skal vi bruke det, og få skjermen til å oppdatere seg av seg
selv når det skjer noe et annet sted i systemet.

Datakilden er Kaffebar-API-et fra samling 3, i ditt eget språk og nå containerisert.
Java-varianten og .NET-varianten følger den samme kontrakten, sender de samme hendelsene
til den samme exchangen og lytter på den samme porten. Frontenden merker ingen forskjell
på dem, og det er nettopp det en kontrakt skal gi oss.

Selve frontenden ligger klar i `kaffebar-web/`. Den er ferdig stylet, henter data over
HTTP og kjører med én gang du gjør `yarn dev`. Du skal ikke bruke tid på CSS eller på
`fetch` i dag.

Det som mangler, er den live forbindelsen. Den skriver du selv, i to runder.

Målet for dagen er to skjermer ved siden av hverandre: du bestiller en kaffe i den ene og
ser bestillingen flytte seg mellom kolonnene i den andre, uten å laste siden på nytt.

---

## Slik bruker vi tiden

| | Tid | Hva | Trenger du Docker? |
| --- | --- | --- | --- |
| **Runde 1** | 12 min | Oppgave 1: koble skjermen på en falsk hendelsesstrøm | **Nei** |
| **Sjekkpunkt** | 2 min | Alle skal ha live oppdatering før vi går videre | |
| **Runde 2** | 31 min | Oppgave 2: bytt til ekte hendelser fra RabbitMQ | Ja |

> ⏱ **Oppgave 1 og 2 er obligatoriske. Oppgave 3 og utover er ekstraoppgaver.** Blir du ikke ferdig med en
> oppgave, kopierer du inn `-solution`-fila og går videre. Fasiten er der for å holde oss
> samlet i tid, ikke for å ta deg.
>
> Oppgave 3 til 6 trenger ingen ny infrastruktur, så blir du tidlig ferdig, er det bare å
> begynne på dem.
>
> Står du fast, kan du bruke en AI-agent som hjelper i stedet for fasiten.
> [`AI-HJELPER.md`](AI-HJELPER.md) inneholder en ferdig prompt du limer inn: den stiller
> spørsmål i stedet for å skrive koden for deg, og sender deg til fasiten når du ber om
> det.
>
> Vi går **ikke** gjennom løsningen i plenum. Fasitfilene er derfor skrevet slik at de kan
> leses alene: hver av dem forklarer valgene sine, og `src/app/api/events/route.ts`
> forklarer hvorfor hver ferdige bit ligger der. Les dem når du er ferdig, eller på veien
> hjem.

---

## Før samlingen

Sett av et kvarter til dette i god tid, ikke samme ettermiddag. Da bruker vi tiden i
rommet på koding i stedet for på nedlasting.

**1. Hent avhengighetene**

```bash
cd 4-Frontend/kaffebar-web
corepack enable
yarn install
```

**2. Bygg og start containerne én gang**, fra rota av repoet:

```bash
docker compose --profile java up --build      # eller --profile dotnet
```

Første gang tar dette noen minutter, siden Maven eller NuGet skal laste ned alt. Etterpå
ligger både base-images og byggcache på maskinen din, og oppstarten går på sekunder.

Sjekk at API-et svarer på http://localhost:8080/menu, og at http://localhost:8080/health
sier `"rabbitmq": "connected"`. Da vet du at både API-et og broker er oppe. Stopp det
igjen med `docker compose down` når du har sett det.

**3. Sjekk at frontenden starter**

```bash
cd 4-Frontend/kaffebar-web
USE_MOCK_API=true yarn dev
```

Ser du en kaffemeny på http://localhost:3000, er du klar. Får du det ikke til, si fra på
forhånd, så fikser vi det før samlingen i stedet for underveis.

Du trenger Node 24 LTS (ikke 26, den er fortsatt Current), Docker Desktop og en editor med
TypeScript-støtte.

---

## Runde 1: live oppdatering uten containere

Ingen Docker, ingen RabbitMQ og ingen amqplib i denne runden. Målet er at alle skal se en
hendelse endre noe på skjermen i løpet av det første kvarteret, før vi kobler på
infrastruktur. Da vet du at klientsiden din virker, og at det er rørene som feiler hvis
noe ryker i runde 2.

Start appen i mock-modus:

```bash
cd 4-Frontend/kaffebar-web
USE_MOCK_API=true yarn dev
```

Åpne http://localhost:3000 (kundeskjerm) og http://localhost:3000/barista (baristaskjerm)
i hvert sitt vindu. Dataene vises, men ingenting beveger seg, og indikatoren øverst til
høyre sier «frakoblet». Slik skal det være nå.

### Oppgave 1: Koble skjermen på hendelsesstrømmen

* **Mål:** få en hendelse som kommer inn over nettverket til å ende som en endring på
  skjermen.
* **Oppgave:** åpne `src/hooks/useEventSource.ts` og skriv hooken. Opprett en
  `EventSource` mot `url`, kall `onEvent` for hver hendelse, sett tilstanden til `"open"`
  når forbindelsen er oppe, og lukk forbindelsen når komponenten forsvinner.

  Hooken peker allerede på `/api/events/demo`, en ferdig rute som sender en hendelse
  annethvert sekund. I `src/hooks/useOrders.ts` ser du hva som skjer videre: den
  invaliderer cachen i TanStack Query, og alt som viser bestillinger henter seg selv på
  nytt.
* **Verdt å vite:**
  - `onEvent` er en ny funksjon for hver render. Legger du den i avhengighetslista til
    `useEffect`, kobler du ned og opp igjen hver eneste gang komponenten rendres. Bruk en
    `ref`. Dette er en klassisk React-felle, og den er verdt å kjenne igjen.
  - `EventSource` ligger innebygd i nettleseren. Du trenger ingen pakke og ingen
    konfigurasjon.
  - React kjører effekter to ganger i utviklingsmodus (StrictMode). At du ser to
    tilkoblinger i nettverksfanen lokalt, er forventet.
  - Vi invaliderer cachen i stedet for å skrive `data` fra hendelsen rett inn i den. Det
    koster litt mer nettverkstrafikk, men vi slipper å håndtere hendelser som kommer i
    feil rekkefølge. Tenk gjerne over når det er verdt å bytte.
* **Fasit:** `src/hooks/useEventSource-solution.ts`

Ferdig før de andre? Begynn på oppgave 4 med det samme. Den virker like fint mot
demo-strømmen som mot den ekte.

### ✅ Sjekkpunkt

Beveger kolonnene på `/barista` seg av seg selv, og står det «live» øverst til høyre? Da
har du hele kjeden på plass, fra hendelse til skjerm. Vi venter på hverandre her, så si
fra hvis du står fast. Det gjelder også dere som sitter i regionene.

---

## Runde 2: bytt den falske kilden med den ekte

Nå kobler vi på infrastrukturen. Setningen å ta med seg videre er denne:

> **Serversiden i metarammeverket ditt er BFF-en din.**

Du trenger ingen egen BFF-tjeneste. Route handleren i Next-appen abonnerer på RabbitMQ med
`amqplib`, på samme måte som consumeren du skrev i samling 2, og sender hendelsene videre
til nettleseren som den samme tekststrømmen du koblet deg på i runde 1. Nettleseren ser
aldri en AMQP-pakke, og den trenger hverken brukernavn eller passord til broker.

### 1. Start API-et og broker

Kjør én av disse fra rota av repoet. Ikke begge, for de deler port 8080:

```bash
docker compose --profile java   up --build     # Java-sporet
docker compose --profile dotnet up --build     # .NET-sporet
```

Kommandoen starter RabbitMQ og fasit-API-et fra samling 3, og ingenting mer. Første bygg
tar noen minutter, siden Maven eller NuGet skal laste ned alt.

> **Kom du ikke i mål i mai?** Det er nettopp derfor fasiten ligger her. Profilene over
> bygger `3-APIs/kaffebar-java-solution` og `3-APIs/kaffebar-dotnet-solution`, som er
> komplette implementasjoner av kontrakten. `FASIT.md` forklarer hver enkelt oppgave fra
> mai. Du trenger altså ikke ha fullført noe for å henge med i dag.
>
> Vil du heller bruke din egen kode, se oppgave 8.

Sjekk at API-et svarer:

| | Java | .NET |
| --- | --- | --- |
| Menyen | http://localhost:8080/menu | samme |
| Kontrakten (JSON) | http://localhost:8080/openapi.json | samme |
| Utforsk API-et | http://localhost:8080/swagger-ui.html | http://localhost:8080/scalar/v1 |
| Helsesjekk | http://localhost:8080/health | samme |
| RabbitMQ | http://localhost:15672 (guest / guest) | samme |

Åpne `/health`. Står det `"rabbitmq": "connected"`, sender API-et hendelser, og
auto-baristaen flytter bestillinger videre hvert åttende sekund. Det er den som holder
baristaskjermen i bevegelse resten av dagen.

### 2. Bytt til ekte data

Stopp dev-serveren og start den uten mock:

```bash
yarn dev
```

Endre så én linje i `src/hooks/useOrders.ts`:

```diff
- const EVENTS_URL = "/api/events/demo";
+ const EVENTS_URL = "/api/events";
```

Nå står skjermen stille igjen, og indikatoren går tilbake til «Frakoblet». Ruta
`/api/events` finnes, men abonnementet under den er ikke skrevet, og serveren sier fra om
det i stedet for å late som alt er i orden. Det er oppgave 2.

### Oppgave 2: Abonner på RabbitMQ

* **Mål:** gjøre en meldingskø om til noe nettleseren allerede forstår.
* **Oppgave:** åpne `src/lib/rabbitmq.ts`. Der ligger tre TODO-er:
  1. `assertExchange`. Exchangen heter `kaffebar` og er av typen `topic`.
  2. `assertQueue` og `bindQueue`. Du lager din egen eksklusive kø og binder den med en
     routing key som fanger `order.created` og `order.status-changed`.
  3. `consume`. Tolk meldingen og send den videre til `onEvent`.

  Det er de samme kallene som i april. Resten ligger ferdig: tilkoblingen, opprydningen og
  route handleren i `src/app/api/events/route.ts` som gjør hendelsene om til Server-Sent
  Events.
* **Verdt å vite:**
  - `order.*` eller `order.#`? I AMQP er det punktum som skiller leddene, ikke bindestrek,
    så begge treffer her. Velg én, og tenk over hvorfor.
  - Åpne http://localhost:15672. Køen din dukker opp når du kobler til, og forsvinner når
    du lukker fanen. Det er `exclusive: true` som sørger for det.
  - Du kan teste strømmen uten nettleser. Kjør `curl -N localhost:3000/api/events` i én
    terminal mens du bestiller i en annen.
  - Hva skjer hvis RabbitMQ er nede? Prøv `docker compose stop rabbitmq`. Både API-et og
    siden skal fortsatt virke: API-et svarer `"rabbitmq": "disconnected"` på `/health` og
    tar imot bestillinger som før, og frontenden sier fra at den ikke er live i stedet for
    å henge.
  - Legg merke til `globalThis`-vakten rundt tilkoblingen. Den ligger ferdig, og uten den
    ville hot reload gitt deg en ny AMQP-tilkobling hver gang du lagrer. Etter en halvtimes
    koding kommer hver hendelse da et titalls ganger, og det ser ut som en feil i din egen
    kode.
  - Tenk over hvorfor vi bruker SSE og ikke WebSocket her. Dataene går bare én vei.
* **Fasit:** `src/lib/rabbitmq-solution.ts`

### ✅ Når det virker

Åpne `/` i ett vindu og `/barista` i et annet, side om side, og bestill en kaffe.
Bestillingen skal dukke opp i køen i det andre vinduet og flytte seg videre av seg selv
når auto-baristaen jobber. Da er du i mål med det obligatoriske.

Bruk gjerne fem minutter på dette når du er ferdig, eller når tiden er ute:

1. Sammenlign din `rabbitmq.ts` med `rabbitmq-solution.ts`. Valgte du `order.*` eller
   `order.#`, og hva skjer med køen din når du lukker fanen?
2. Les `src/app/api/events/route.ts` ovenfra og ned. Den er ferdig skrevet, men
   kommentarene forklarer hvorfor hver enkelt bit er der. Det er de bitene som pleier å
   koste en ettermiddag første gang du skriver SSE selv.
3. Se på `src/hooks/useOrders.ts`. Én invalidering, og alt som viser bestillinger henter
   seg selv på nytt. Det er grunnen til at hooken din var verdt arbeidet.

Og har du tid igjen: oppgave 3 til 6 under trenger hverken nye containere eller kode fra
mai, så der kan du bare sette i gang.

---

## Ekstraoppgaver

Oppgave 3 til 6 trenger ingen ny infrastruktur og ingen kode fra mai. Er du ferdig med
det obligatoriske, kan du begynne på dem med én gang, også mens du venter på et
Docker-bygg. Oppgave 7 til 11 krever litt mer. Ta dem i den rekkefølgen du vil, og husk at
miljøet blir stående, så du kan gjøre resten hjemme.

### Oppgave 3: Overlev en storm av hendelser

Åtte sekunder mellom hver hendelse er en snill verden. Hva skjer når det kommer femti i
sekundet?

* **Mål:** et UI som holder seg oppe når hendelsene kommer fortere enn du rekker å hente
  data.
* **Oppgave:** skru ned intervallet på auto-baristaen og start API-et på nytt. I
  `docker-compose.yml` setter du `KAFFEBAR_BARISTA_INTERVALMS=200` (Java) eller
  `Kaffebar__Barista__IntervalMs=200` (.NET). Jobber du i mock-modus, endrer du
  `AUTO_BARISTA_INTERVAL_MS` i `src/mocks/events.ts`.

  Se på nettverksfanen. Hver hendelse utløser en full henting av `/api/orders`, og
  rendringen henger etter. Fiks det: samle opp hendelsene og hent lista maks én gang i
  sekundet, uansett hvor mange som kommer.
* **Verdt å vite:**
  - Hvor er flaskehalsen? Nettverket, React-rendringen, eller begge deler?
  - Skal du hente med én gang den første hendelsen kommer, eller vente og se om det
    kommer flere? De to svarene gir helt ulik opplevelse på en treg forbindelse.
  - `staleTime` i TanStack Query løser ikke dette alene. Hvorfor ikke?
* **Fasit:** `src/hooks/useOrders-solution.ts`

### Oppgave 4: Skriv hendelsen rett inn i cachen

Hendelsen inneholder hele bestillingen. Vi kaster den og henter alt på nytt. Det er
kjedelig og riktig, men det er ikke det eneste svaret.

* **Mål:** oppdatere skjermen uten et eneste nettverkskall.
* **Oppgave:** bytt ut invalideringen i `useOrders.ts` med `queryClient.setQueryData`, og
  skriv `event.data` rett inn i lista. Da må du håndtere det invalideringen skjulte for
  deg: hendelser som kommer i feil rekkefølge. Bruk `updatedAt` på bestillingen som vakt,
  så en forsinket hendelse ikke flytter en ferdig kaffe tilbake i køen.
* **Verdt å vite:**
  - Behold gjerne en sjelden full henting som sikkerhetsnett, i tilfelle du mister en
    hendelse mens fanen lå i bakgrunnen.
  - Vil du gå videre: gjør baristaknappene optimistiske. Flytt bestillingen i cachen med
    én gang, og rull tilbake hvis kallet feiler. Fasiten viser mønsteret med `onMutate`,
    `onError` og `onSettled`.
  - Tenk over hvorfor invalidering likevel er det riktige standardsvaret i de fleste
    prosjekter.
* **Fasit:** `src/hooks/useOrders-solution.ts`

### Oppgave 5: Vis tydelig når forbindelsen ryker

En skjerm som viser gamle tall uten å si fra, er verre enn en som er ærlig.

* **Mål:** brukeren skal aldri være i tvil om dataene er ferske.
* **Oppgave:** stopp broker med `docker compose stop rabbitmq`, eller stopp dev-serveren
  hvis du står i mock-modus. Hooken din vet allerede hva som skjer, for den returnerer en
  `ConnectionState`. Bruk den: si fra i grensesnittet at oppdateringene er borte, og hent
  alt på nytt når forbindelsen er tilbake.
* **Verdt å vite:**
  - `EventSource` kobler opp igjen av seg selv, og serveren lukker strømmen når
    abonnementet dør, nettopp for å utløse det. Men tilstanden på serveren har endret seg
    mens du var nede, så hva må skje i det øyeblikket du er tilbake?
  - Hvordan vil du vise det? Et banner, en dempet farge på lista, eller et «sist
    oppdatert»-tidsstempel? Prøv minst to og se hva som faktisk er til å forstå.
  - Start broker igjen med `docker compose start rabbitmq` og se at det retter seg selv.
* **Fasit:** ingen. Her finnes det flere gode svar, og valget er et designvalg like mye
  som et teknisk et.

### Oppgave 6: Skriv en test

* **Mål:** teste oppførsel, ikke implementasjon.
* **Oppgave:** `__tests__/components.test.tsx` viser mønsteret, og `yarn test` kjører det.
  Skriv en test til. To forslag: at en bestilling havner i riktig kolonne på
  baristaskjermen, eller at logikken din fra oppgave 4 lar en ny hendelse vinne over en
  gammel.
* **Verdt å vite:**
  - `EventSource` finnes ikke i jsdom. Hvordan tester du en hook som er avhengig av noe
    nettleseren har, men testmiljøet ikke har?
  - MSW ligger allerede i repoet, så du kan mocke nettverket i stedet for å mocke
    `fetch` selv.
  - Testene er raske. Kjør `yarn test:watch` mens du jobber.

### Oppgave 7: Containeriser frontenden
* **Oppgave:** skriv `kaffebar-web/Dockerfile`. Bruk multi-stage, etter samme mønster som
  Dockerfilene for Kaffebar-API-et i `3-APIs/kaffebar-*-solution/`: bygg i et image med
  hele verktøykassa, og kjør i et som bare har det du trenger. Kjør så:

  ```bash
  docker compose --profile java --profile frontend up --build
  ```

* **Verdt å vite:** `next.config.ts` setter `output: "standalone"`. Hva betyr det for
  runner-steget, hva slipper du å kopiere, og hvor mye mindre blir imaget? Sammenlign
  `docker images` før og etter.
* **Fasit:** `kaffebar-web/Dockerfile-solution`

### Oppgave 8: Bruk din egen kode fra mai
Dette er både belønningen hvis du kom langt i mai, og den ærligste testen kontrakten din
kan få.

* **Oppgave:** stopp fasit-containeren, som okkuperer port 8080 (`docker compose stop
  kaffebar-java` eller `kaffebar-dotnet`). Start ditt eget API fra `3-APIs/kaffebar-java`
  eller `3-APIs/kaffebar-dotnet`, og pek frontenden på det:

  ```bash
  KAFFEBAR_API_URL=http://localhost:5035 yarn dev      # .NET på 5035
  KAFFEBAR_API_URL=http://localhost:8080 yarn dev      # Java på 8080
  ```

* **Verdt å vite:**
  - Hva mangler? Implementerte du `GET /orders` med filtrering, og sender du `coffeeName`
    på bestillingen? Frontenden sier ganske presist fra om hva kontrakten din ikke dekker,
    og det er en ærligere test enn Swagger-UI.
  - Valgte du `POST /orders/{id}/status` i stedet for `PATCH` i oppgave 7? Fasiten støtter
    begge deler, men det gjør kanskje ikke din. Da må du endre én linje i
    `updateOrderStatus`.
  - Regenerer typene mot ditt eget API. Det serverer neppe kontrakten på `/openapi.json`,
    for det aliaset la vi inn i fasiten. Bruk den ekte stien:
    `npx openapi-typescript http://localhost:8080/v3/api-docs -o src/types/kaffebar.ts`
    (Java) eller `.../openapi/v1.json` (.NET). Kompilerer appen fortsatt?
  - Sender ditt API i det hele tatt hendelser? Hvis ikke står baristaskjermen stille, og
    da er det en god anledning til å se på `events/`-pakken i fasiten.

### Oppgave 9: Koble på consumeren fra samling 2
* **Oppgave:** la consumeren du skrev i april abonnere på `order.*`-hendelsene på
  `kaffebar`-exchangen og lagre dem i Postgres:

  ```bash
  docker compose --profile java --profile consumer up --build java-consumer
  ```

  Da har du en frontend og en consumer som lytter på de samme hendelsene, uten å vite om
  hverandre.
* **Verdt å vite:** hvem eier hendelsen? Og hva skjer med consumeren hvis frontenden
  kobler seg fra? Ingenting, og det er nettopp poenget med en topic exchange.

### Oppgave 10: Se på typene
`yarn generate:types` kjører `openapi-typescript` mot `/openapi.json` og skriver
`src/types/kaffebar.ts`. Fila ligger ferdig generert i repoet, men kjør kommandoen mot
ditt eget spor og se at du får noe strukturelt likt. Poenget er at typene i frontenden
ikke er noe noen vedlikeholder, de faller ut av kontrakten.

Sitter du i Java-sporet, kan du åpne `config/OpenApiConfiguration.java` i fasiten og se
hvorfor `ModelResolver.enumsAsRef` måtte skrus på. Uten den hadde du ikke fått noen
`OrderStatus`-type i det hele tatt, bare løse strenger i hvert felt.

### Oppgave 11: La en LLM skrive menyen
Denne krever din egen API-nøkkel, så den egner seg best hjemme. Vi deler ikke ut nøkler i
dag, og samling 6 tar AI-delen skikkelig.

* **Oppgave:** få en LLM til å lage fristende produktbeskrivelser av kaffene på menyen, og
  vis dem på kundeskjermen.
* **Verdt å vite:** hvor hører det hjemme, i API-et, i BFF-en eller i nettleseren? Hva
  koster hvert av valgene i responstid, og hvem får se API-nøkkelen din?

---

## Hvis Docker ikke vil

`USE_MOCK_API=true yarn dev` lar appen svare fra et lager i minnet i stedet for fra
Kaffebar-API-et: samme meny, samme statusflyt og en auto-barista som flytter bestillinger
hvert åttende sekund.

Runde 1 fungerer i sin helhet uten containere. Runde 2 trenger en broker å abonnere på, så
får du ikke containerne opp: gjør oppgave 1, og les fasiten for oppgave 2. Den er skrevet
for å kunne leses uten at noen forklarer den. Miljøet blir stående, så du kan ta resten
hjemme.

---

## Feilsøking

| Symptom | Mest sannsynlig |
| --- | --- |
| «Får ikke kontakt med Kaffebar-API-et» på siden | Containeren er ikke oppe, eller du glemte `USE_MOCK_API=true` |
| Siden viser data, men ingenting beveger seg | Hooken (oppgave 1), eller feil verdi i `EVENTS_URL` |
| `curl -N localhost:3000/api/events` gir ingen output | Abonnementet (oppgave 2). Uten binding kommer det ingen meldinger |
| Hendelsene kommer flere ganger | Hot reload. Restart `yarn dev` |
| To tilkoblinger i nettverksfanen | StrictMode i utviklingsmodus. Forventet |
| Port 3000 er opptatt | Noe annet kjører der. Sjekk med `lsof -i :3000` |

---

## Miljøvariabler

| Variabel | Standard | Hva den gjør |
| --- | --- | --- |
| `KAFFEBAR_API_URL` | `http://localhost:8080` | Hvilket Kaffebar-API frontenden snakker med. Pek den på ditt eget i oppgave 8 |
| `RABBITMQ_URL` | `amqp://guest:guest@localhost:5672` | Broker. Brukes bare av `/api/events` |
| `RABBITMQ_EXCHANGE` | `kaffebar` | Exchangen vi abonnerer på |
| `USE_MOCK_API` | `false` | Kjører appen uten containere |

---

## Oppsummering

Etter denne workshopen har du vært innom:

- **Server-Sent Events**, den enkleste måten å sende data fra server til nettleser, og
  oftest den eneste du trenger.
- **Metarammeverket som BFF**: én route handler erstattet en hel tjeneste.
- **TanStack Query**: én invalidering, og alt som viser dataene oppdaterer seg.
- **Kontrakten helt ut i UI-et**: typene i frontenden kommer fra den samme kontrakten som
  drev API-et du bygde i mai, uansett om det var Java eller .NET.
- **Server components**: datahenting på serveren, uten spinner og uten `useEffect`.
- At kode, CI, image, compose, API, hendelser og UI henger sammen i én kjede, og at du nå
  har skrevet kode i alle leddene.

Anbefalingene vi går gjennom etterpå, ligger i [`BEST-PRACTICE.md`](BEST-PRACTICE.md).

Lykke til, og god kaffe! ☕
