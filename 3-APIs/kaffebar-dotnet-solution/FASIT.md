# Fasit — Kaffebar, .NET-sporet (samling 3)

Dette er løsningen på oppgave 1 til 10 i
[`../kaffebar-dotnet/oppgave.md`](../kaffebar-dotnet/oppgave.md), som en komplett,
kjørbar .NET 10-løsning.

Dokumentet følger **oppgavene**, ikke koden. Én seksjon per oppgave, i samme rekkefølge
som oppgave.md, slik at det kan brukes som manus når vi går gjennom løsningen på
storskjerm.

```bash
cd 3-APIs/kaffebar-dotnet-solution/Kaffebar
dotnet run                      # http://localhost:5035, åpner Scalar

cd ..
dotnet test                     # 20 tester
```

| URL | Hva |
| --- | --- |
| http://localhost:5035/menu | Menyen |
| http://localhost:5035/scalar/v1 | Scalar — utforsk og test alle endepunkter |
| http://localhost:5035/openapi/v1.json | Spesifikasjonen rammeverket genererer av koden |

Bruk gjerne [`Kaffebar/Kaffebar.http`](Kaffebar/Kaffebar.http) i stedet for curl. Den
dekker alle endepunktene, inkludert feiltilfellene, og er det raskeste verktøyet i
rommet når noen lurer på hva API-et svarer.

**Starteren er urørt.** `3-APIs/kaffebar-dotnet/` står nøyaktig som du klonet den. Alt
her ligger i en søskenmappe, så du kan ha begge åpne samtidig og diffe dem mot
hverandre.

---

## Det store bildet først

.NET-sporet er **code-first**: du skriver C#, og rammeverket utleder
OpenAPI-spesifikasjonen. Java-sporet er **contract-first**: kontrakten er kilden, og
koden faller ut av den.

Forskjellen høres akademisk ut helt til du prøver å få de to til å svare likt. Da blir
den veldig konkret, og det er den beste grunnen til å lese denne fasiten sammen med
[`../kaffebar-java-solution/FASIT.md`](../kaffebar-java-solution/FASIT.md).

Kortversjonen: **code-first gir deg riktig oppførsel nesten gratis, og omtrentlig
dokumentasjon.** Alt i `Kaffebar/OpenApi/` finnes fordi den utledede spesifikasjonen
ikke helt beskrev det API-et faktisk gjør. Den mappa har ingen motpart i Java-prosjektet
— der ER YAML-en kontrakten.

### Filene, kort

```
kaffebar-dotnet-solution/
├── Kaffebar/
│   ├── Program.cs            # oppsett + Minimal API-ene (GET /menu, GET /health)
│   ├── Controllers/          # OrdersController — oppgave 2
│   ├── Models/               # record-typer, enums, validering
│   ├── Storage/              # IOrderRepository + ConcurrentDictionary + seed
│   ├── Errors/               # ProblemFactory — RFC 7807, oppgave 6
│   ├── Auth/                 # oppgave 10, AV som standard
│   ├── OpenApi/              # det rammeverket ikke klarer å utlede selv
│   ├── Events/               # valgfritt: RabbitMQ + auto-barista, AV som standard
│   └── Kaffebar.http
├── Kaffebar.Tests/           # 20 WebApplicationFactory-tester
├── Oppgave9/                 # polymorfisme, eget kjørbart prosjekt + NOTAT.md
└── FASIT.md                  # denne
```

### Hva som er lagt til utover starteren

| Pakke | Hvorfor |
| --- | --- |
| `Microsoft.AspNetCore.Mvc.Testing` | Testprosjektet. Var ikke i starteren. |
| `Microsoft.Identity.Web` | Oppgave 10. Autentisering er **av** som standard. |
| `RabbitMQ.Client` | Det valgfrie siste steget. Også **av** som standard. |

Alt annet er identisk med starteren: `net10.0`, nullable enabled, implicit usings,
`Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, port 5035 og
`launchUrl: scalar/v1` — så README-en fra mai stemmer fortsatt.

`Kaffebar` og `Kaffebar.Tests` er registrert i `CodeAcademy2026.sln` i rota, slik at
`dotnet build` derfra dekker dem. `Oppgave9` er bevisst holdt **utenfor** rot-solutionen:
den er et eksperiment, og den skal ikke kunne velte bygget for alle andre.

Lokalt finnes `Kaffebar.slnx`, som inneholder alle tre. Den gjør at `dotnet build` og
`dotnet test` virker fra denne mappa uten å måtte peke på et prosjekt.

> **Én ting til i rot-solutionen:** den pekte på
> `2-EventDriven/dotnet-consumer/DotnetConsumer.csproj`, en fil som ikke finnes lenger.
> `dotnet build` fra rota feilet derfor allerede før denne fasiten ble lagt til. Den
> døde oppføringen er fjernet — de fire ekte prosjektene under `dotnet-consumer/` sto
> allerede registrert hver for seg, så ingenting mangler.

> **Om `NU1903`:** bygget gir en advarsel om at `Microsoft.OpenApi` 2.0.0 har et kjent
> sårbarhet. Pakken kommer transitivt fra `Microsoft.AspNetCore.OpenApi` 10.0.4, og den
> samme advarselen finnes i starteren. Den er ikke innført her, og bør fikses ett sted —
> i starteren — når det finnes en oppdatert versjon.

---

## Oppgave 1: Den første bestillingen (POST)

**Løsningen:** `POST /orders` med `CreateOrderRequest` inn og `Order` ut.

📄 [`Controllers/OrdersController.cs`](Kaffebar/Controllers/OrdersController.cs) — `CreateOrder`
📄 [`Models/Orders.cs`](Kaffebar/Models/Orders.cs)

```csharp
[HttpPost]
public ActionResult<Order> CreateOrder(CreateOrderRequest request)
{
    var coffee = repository.FindCoffee(request.CoffeeId);
    if (coffee is null)
        return ProblemFactory.NotFound(HttpContext, $"Fant ingen kaffe med id …").AsResult();

    var order = repository.Create(request, coffee);
    return Created($"/orders/{order.OrderId}", order);
}
```

**Verdt å stoppe opp ved:**

**To typer, ikke én.** `CreateOrderRequest` og `Order` er ulike `record`-er, og det er
hele poenget med oppgaven. Klienten sender `CoffeeId`; serveren svarer med `OrderId`,
`CoffeeName`, `Status`, `CreatedAt` og `UpdatedAt` — felter klienten verken kan eller
skal sette. Gjenbrukte vi `Order` begge veier, måtte alle de feltene vært nullable, og
kontrakten ville sluttet å si noe presist om noe som helst.

**`Created(...)`, ikke `Ok(...)`.** 201 og en `Location`-header som peker på den nye
ressursen. Det er det «Created» betyr, og det er gratis.

**`record`, ikke `class`.** DTO-er er verdier: de skal sammenlignes på innhold, ikke
identitet, og de skal ikke endres etter at de er laget. `record` gir deg begge deler,
pluss `with` — som `InMemoryOrderRepository.SetStatus` bruker til å lage en kopi med ny
status i stedet for å mutere.

**`CoffeeName` er denormalisert med vilje.** Ordren bærer med seg navnet på kaffen, ikke
bare id-en. Det er et bevisst brudd på «ikke dupliser data»: uten det må frontenden
hente `/menu` for å vise én ordre. Prisen er at ordren ikke endrer navn hvis menyen gjør
det — noe som for en kvittering er riktig oppførsel uansett.

---

## Oppgave 2: Fra Minimal API til Controller

**Løsningen:** `POST /orders` og resten av ordre-endepunktene ligger i
`OrdersController`. `GET /menu` ligger igjen som Minimal API i `Program.cs`.

📄 [`Program.cs`](Kaffebar/Program.cs) — `app.MapGet("/menu", …)`
📄 [`Controllers/OrdersController.cs`](Kaffebar/Controllers/OrdersController.cs)

> **Det ser inkonsekvent ut. Det er poenget.** Oppgaven ber eksplisitt om denne
> blandingen, slik at du kan se de to stilene ved siden av hverandre i samme prosjekt.
> Det står en kommentar begge steder så ingen «rydder» det bort.

### Diskusjon: Minimal API vs Controllers — hva vinner man med hver?

**Minimal API vinner på nærhet.** Hele endepunktet er ett uttrykk:

```csharp
app.MapGet("/menu", (IOrderRepository repository) => repository.Menu)
   .WithSummary("Hent kaffemeny")
   .Produces<IReadOnlyList<Coffee>>();
```

Rute, metode, avhengigheter, logikk og dokumentasjon på fem linjer, uten en eneste
attributt. Det er lite å lete etter og lite å ta feil av. For et endepunkt som gjør én
ting, er det vanskelig å slå.

**Controllers vinner på gjentakelse.** `OrdersController` har fem endepunkter som deler
prefiks (`[Route("orders")]`), deler avhengigheter (konstruktøren), deler feilhåndtering
(`[ProducesResponseType]` på klassen) og deler en privat hjelpemetode (`ApplyStatus`).
Alt det måtte vært skrevet på nytt per endepunkt i et Minimal API, eller trukket ut i
hjelpefunksjoner du selv må finne på et sted for.

`[ApiController]` gir dessuten tre ting gratis som du ellers må be om:
automatisk modellvalidering med 400 før koden din kjører, binding fra body/route/query
uten `[FromBody]` og venner, og ProblemDetails på feil.

### Hvor er valideringen tydeligst?

I controlleren — men ikke fordi Minimal API er dårligere. Det er fordi `[ApiController]`
kjører valideringen *for deg*. I et Minimal API i .NET 10 må du be om den:

```csharp
builder.Services.AddValidation();          // i Program.cs
app.MapPost("/orders", …).WithValidation(); // på endepunktet
```

Vi trenger den ikke her, fordi de to Minimal API-ene våre ikke tar imot noe. Men det er
verdt å vite at den er et valg i den ene stilen og en standard i den andre — og at det
er nettopp sånne små forskjeller som gjør at et prosjekt sklir mot én stil over tid.

### Ser endepunktene like ut i Scalar?

Ja. Det er det viktigste svaret i hele oppgave 2: **stilvalget er internt.** Begge
stilene ender i det samme endepunkts-registeret, og den genererte spesifikasjonen ser
ikke forskjell. En klient ser ikke forskjell.

Forskjellen er hvor lett koden er å lese og vedlikeholde — ikke hva API-et er.

---

## Oppgave 3: Enums og valgfrie felt

**Løsningen:** C#-enums med `JsonStringEnumMemberName`, `JsonStringEnumConverter`
registrert to steder, og nullable reference types til å styre `required`.

📄 [`Models/Enums.cs`](Kaffebar/Models/Enums.cs)
📄 [`Models/Orders.cs`](Kaffebar/Models/Orders.cs)
📄 [`Program.cs`](Kaffebar/Program.cs) — `ConfigureJson` og `ConfigureHttpJsonOptions`

```csharp
public enum OrderStatus
{
    [JsonStringEnumMemberName("PENDING")] Pending,
    [JsonStringEnumMemberName("BREWING")] Brewing,
    [JsonStringEnumMemberName("READY")]   Ready
}
```

**Verdt å stoppe opp ved:**

**Uten `JsonStringEnumConverter` blir `PENDING` til `0`.** Det er den vanligste
enkeltfeilen i .NET-sporet, og den er stille: API-et svarer `200`, JSON-en er gyldig,
og klienten får `"status": 0`. Ingenting feiler før noen prøver å lese det.

**Og den må registreres TO steder.** Controllers serialiseres av MVC sine `JsonOptions`,
Minimal API-er av `ConfigureHttpJsonOptions`. Glemmer du den ene, kommer enums ut som
tall fra halve API-et — og siden `GET /menu` ikke har noen enums, merker du det ikke før
noen kaller `/health`.

**`JsonStringEnumMemberName` i stedet for å døpe om medlemmene.** Kontrakten krever
`PENDING`, C#-konvensjonen sier `Pending`. Attributtet (nytt i .NET 9) lar deg ha begge
deler, og det påvirker både serialiseringen og den genererte spesifikasjonen.

**Valgfritt er ikke det samme som null.** `MilkType?` betyr i C# «kan være null», men
kontrakten sier «kan mangle». Vi har valgt at et felt uten verdi **forsvinner helt** fra
JSON-en:

```csharp
options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
```

Det er også slik referanse-implementasjonen og Java-fasiten oppfører seg, og det gjør at
de tre kan diffes mot hverandre.

### Diskusjon: hvordan styrer nullable reference types hva som blir `required`?

Regelen er enkel og overraskende presis: **en ikke-nullable egenskap uten default-verdi
blir `required` i spesifikasjonen.**

| C# | I `/openapi/v1.json` |
| --- | --- |
| `required string CustomerName { get; init; }` | i `required`-lista |
| `CoffeeSize Size` med `[Required]` | i `required`-lista |
| `MilkType? MilkType { get; init; }` | ikke påkrevd |
| `int Quantity { get; init; } = 1` | ikke påkrevd, med `"default": 1` |

Det betyr at nullability ikke bare er en kompilatorsjekk du kan slå av — **den er en del
av API-kontrakten din**. Fjerner du et spørsmålstegn i C#, endrer du hva klienter må
sende. Det er et sterkt argument for `<Nullable>enable</Nullable>`, og et like sterkt
argument for å lese den genererte spesifikasjonen etter hver endring.

Og det er her contract-first og code-first virkelig skiller lag: i Java-sporet skriver
du `required: [coffeeId, customerName, size]` og MENER det. Her *faller det ut* av
hvordan du tilfeldigvis skrev typen.

---

## Oppgave 4: Validering med Data Annotations

**Løsningen:** `[Required]`, `[StringLength]`, `[RegularExpression]` og `[Range]` på
egenskapene. Ingen `if`-setninger.

📄 [`Models/Orders.cs`](Kaffebar/Models/Orders.cs)
📄 [`Errors/ProblemFactory.cs`](Kaffebar/Errors/ProblemFactory.cs)
📄 [`Program.cs`](Kaffebar/Program.cs) — `InvalidModelStateResponseFactory`

```csharp
[Required(ErrorMessage = "customerName er påkrevd.")]
[StringLength(50, MinimumLength = 2, ErrorMessage = "customerName må være mellom 2 og 50 tegn.")]
[RegularExpression(@"^(?!\s*$).+$", ErrorMessage = "customerName kan ikke være blankt.")]
public required string CustomerName { get; init; }
```

og det havner i spesifikasjonen som

```json
"customerName": { "type": "string", "minLength": 2, "maxLength": 50, "pattern": "^(?!\\s*$).+$" }
```

**Verdt å stoppe opp ved — tre feller på rad, og alle tre er ekte.**

**1. Posisjonelle records og validering går ikke sammen.** Dette er den som koster mest
tid. Skriver du

```csharp
public record CreateOrderRequest([property: Range(1, 10)] int Quantity = 1);
```

får du **500** ved første request, ikke 400:

```
Record type has validation metadata defined on property 'Quantity' that will be ignored.
'Quantity' is a parameter in the record primary constructor and validation metadata
must be associated with the constructor parameter.
```

Flytter du attributtet til parameteret (`[Range(1, 10)] int Quantity = 1`), virker
valideringen — men da **forsvinner `minLength`, `maxLength` og `pattern` fra den
genererte spesifikasjonen**, fordi skjemageneratoren leser egenskaper, ikke
konstruktør-parametere. Du får altså velge mellom at reglene håndheves og at de
dokumenteres.

Løsningen er å droppe den posisjonelle formen for request-typer og bruke
`init`-egenskaper. Da får du begge deler. `Order` (som ikke valideres) er fortsatt
posisjonell.

**2. `[RegularExpression]` og mønsteret `\S`.** Kontrakten deles med Java-sporet, og der
håndheves `pattern` av Jakarta Bean Validation, som krever at HELE strengen matcher.
`\S` ville derfor betydd «nøyaktig ett ikke-blankt tegn» i Java. `^(?!\s*$).+$` betyr
det samme i begge verdener. Detaljer i
[Java-fasiten, oppgave 4](../kaffebar-java-solution/FASIT.md).

**3. `errors` har feil form ut av boksen.** `[ApiController]` svarer som standard med
`ValidationProblemDetails`, der `errors` er en **ordbok**:

```json
{ "errors": { "CustomerName": ["The CustomerName field is required."] } }
```

Kontrakten i `4-Frontend/kaffebar-api` bruker en **liste av objekter**:

```json
{ "errors": [ { "field": "customerName", "message": "customerName er påkrevd." } ] }
```

Begge er lovlige RFC 7807. Men den ene er kontrakten, og frontenden i samling 4 gjør
`problem.errors?.map(e => `${e.field}: ${e.message}`)` — som krasjer på en ordbok.
Derfor overstyrer vi `InvalidModelStateResponseFactory`. Legg også merke til at nøklene
i `ModelState` er C#-navn (`CustomerName`) og må mappes til camelCase.

**Dette er prisen for code-first i én bolk:** rammeverkets standardform er ikke
kontraktens form, og du oppdager det først når noen andre prøver å konsumere deg.

**4. En ugyldig enum kommer ikke engang til valideringen.** `"size": "HUGE"` stopper i
System.Text.Json, før modellen finnes. Meldingen derfra er

```
The JSON value could not be converted to Kaffebar.Models.CoffeeSize.
Path: $.size | LineNumber: 0 | BytePositionInLine: 85.
```

— den lekker byte-posisjoner ut til klienten, så vi bytter den ut med `"har en ugyldig
verdi."`. Merk kontrasten: Java-fasiten kan svare *«må være en av SMALL, MEDIUM,
LARGE»*, fordi den kan lese de lovlige verdiene ut av den genererte enum-klassen.
System.Text.Json gir oss ikke måltypen på en form vi kan bruke.

---

## Oppgave 5: Hent en spesifikk bestilling (Route Parameters)

**Løsningen:** `[HttpGet("{orderId:guid}")]`.

📄 [`Controllers/OrdersController.cs`](Kaffebar/Controllers/OrdersController.cs) — `GetOrder`

**Verdt å stoppe opp ved:** `:guid` er en **route-constraint**, ikke en type-annotering.
Den avgjør om ruten matcher i det hele tatt. Det har en konsekvens de fleste ikke
forventer.

### Diskusjon: hva skjer hvis kallet inneholder en ugyldig Guid?

```bash
curl -i localhost:5035/orders/ikke-en-guid
```

**Du får 404.** Ikke 400.

Ruten `orders/{orderId:guid}` matcher ikke, og da finnes det ikke noe endepunkt å gi en
400 fra. Så langt ASP.NET Core er bekymret, spurte du etter noe som ikke finnes.

**Java-sporet og TypeScript-fasiten svarer 400** på nøyaktig det samme kallet. Der er
`orderId` en `String` i ruten, endepunktet treffes, og konverteringen til `UUID` feiler
*inne i* håndteringen — og en konverteringsfeil er klientens skyld, altså 400.

**Hvem har rett?** Begge, og det er poenget:

- **404-argumentet:** `/orders/ikke-en-guid` er ikke en ressurs i dette API-et. Den har
  aldri eksistert og kan aldri eksistere. 404 er en presis beskrivelse.
- **400-argumentet:** klienten har sendt en syntaktisk ugyldig verdi for en parameter
  API-et selv har definert som en uuid. Å svare «finnes ikke» skjuler at problemet er i
  requesten, ikke i datasettet. 400 er mer hjelpsomt.

**Fasiten beholder 404**, fordi det er det idiomatiske i .NET, fordi det er hva en
deltaker som fulgte oppgaveteksten faktisk endte opp med, og fordi det gjør forskjellen
synlig i stedet for å skjule den.

**Vil du ha 400 i stedet?** Fjern constrainten og gjør konverteringen selv:

```csharp
[HttpGet("{orderId}")]
public ActionResult<Order> GetOrder(string orderId)
{
    if (!Guid.TryParse(orderId, out var id))
        return ProblemFactory.Validation(HttpContext,
            [new ValidationError("orderId", "må være en gyldig guid.")]).AsResult();
    …
}
```

Du mister da rutingen som dokumentasjon, og du må skrive parsingen for hvert endepunkt.
Det er en reell avveining, ikke et opplagt valg.

> Merk at forskjellen ikke gjør noe for frontenden i samling 4: den sender aldri en
> ugyldig guid. Den gjør noe for et menneske som debugger med curl.

---

## Oppgave 6: Standardisert feilhåndtering

**Løsningen:** `AddProblemDetails()`, `UseStatusCodePages()`, og all feilbygging samlet
i `ProblemFactory`.

📄 [`Errors/ProblemFactory.cs`](Kaffebar/Errors/ProblemFactory.cs)
📄 [`Program.cs`](Kaffebar/Program.cs) — `AddProblemDetails(… CustomizeProblemDetails …)`
📄 [`OpenApi/ProblemDetailsTransformer.cs`](Kaffebar/OpenApi/ProblemDetailsTransformer.cs)

**Verdt å stoppe opp ved:**

**.NET har RFC 7807 innebygd.** Du trenger ikke definere `Problem`-skjemaet selv, slik
Java-sporet måtte. `ProblemDetails` finnes i rammeverket, `application/problem+json`
settes automatisk, og `AddProblemDetails()` + `UseStatusCodePages()` gjør at også feil
som aldri når koden din — 404 fra en rute som ikke matcher, 401 fra autentiseringen —
kommer ut i samme form.

**`CustomizeProblemDetails` er der du gjør dem til *dine*.** Uten den peker `type` på
RFC-lenker og `title` er på engelsk. Med den får alle feil fra alle lag den samme
`https://codeacademy.soprasteria.no/problems/…`-adressen som de to andre
implementasjonene.

**`[Produces("application/json")]` på controlleren ødelegger media typen på feilsvar.**
Attributtet setter `ContentTypes` på resultatet og overstyrer `application/problem+json`
som `ProblemFactory` ber om. Symptomet er at 404-svaret ditt har riktig body og feil
`Content-Type` — og at en klient som sjekker media typen (slik `api-solution.ts` i
kaffebar-web gjør) ikke kjenner igjen feilen. Derfor står det attributtet ikke på
`OrdersController`.

**`errors` kommer ikke med i spesifikasjonen av seg selv.** Feltet ligger i
`ProblemDetails.Extensions`, som er en ordbok — typen sier ingenting om hva som havner
der, så generatoren kan ikke utlede det. API-et sender det uansett, så uten en
transformer lyver spesifikasjonen ved utelatelse. Det er hva
`OpenApi/ProblemDetailsTransformer.cs` finnes for.

---

## Oppgave 7: Baristaens oppdatering (PATCH vs PUT)

**Løsningen:** `PATCH /orders/{orderId}` som anbefalt variant, og
`POST /orders/{orderId}/status` som alias. Begge tar `UpdateOrderStatusRequest`.

📄 [`Controllers/OrdersController.cs`](Kaffebar/Controllers/OrdersController.cs) — `UpdateOrderStatus`, `SetOrderStatus`, `ApplyStatus`

### Diskusjon: PUT, PATCH eller action-endepunkt?

**`PATCH` er anbefalingen.** Argumentet er kort: vi endrer *én egenskap* på en ressurs
som finnes fra før. Det er nøyaktig det `PATCH` betyr.

**`PUT` ville vært feil her**, ikke bare mindre elegant. `PUT` betyr «erstatt ressursen
med dette». Da må klienten sende hele ordren — inkludert `CreatedAt`, `CustomerName` og
`OrderId` — og serveren må bestemme hva den gjør når de ikke stemmer. Ignorere dem
stille? Feile? Overskrive? Alle tre svarene er dårlige. Og en barista som skal trykke
«ferdig» har ingen grunn til å ha hele ordren i hånda.

**`POST /orders/{id}/status` er ikke galt.** Action-endepunkter er utbredt, de leses
godt, og de er det opplagte valget når operasjonen ikke er en tilstandsendring på ett
felt men en *handling* med sideeffekter — `POST /orders/{id}/refund`, for eksempel.
Prisen er at du har lagt til en ressurs (`/status`) som egentlig ikke er en ressurs, og
at du ikke lenger kan lese HTTP-metoden og vite hva som skjer.

**Hvorfor fasiten støtter begge:** i mai valgte dere ulikt. Skal frontenden i samling 4
kunne peke på deres eget API uten kodeendringer, må begge variantene finnes. Det er en
workshop-begrunnelse, ikke en API-designbegrunnelse — i et ekte API velger du én.

### Hvorfor et eget request-objekt?

```csharp
public record UpdateOrderStatusRequest([Required] OrderStatus Status);
```

Fordi det gjør en hel klasse feil **umulig**, i stedet for bare forbudt.

Gjenbruker du `Order` som request-type, kan en klient sende med `CreatedAt` eller
`CustomerName`, og du må skrive kode som ignorerer dem. Den koden kan glemmes. Med en
type som bare har `Status`, finnes ikke feltene å sende.

Det er det samme argumentet som i oppgave 1: request og response er ulike ting, selv når
de handler om det samme.

---

## Oppgave 8: Filtrering og paginering (Query Parameters)

**Løsningen:** `status`, `limit` og `offset` som parametere med default-verdier og
`[Range]`.

📄 [`Controllers/OrdersController.cs`](Kaffebar/Controllers/OrdersController.cs) — `ListOrders`
📄 [`Storage/InMemoryOrderRepository.cs`](Kaffebar/Storage/InMemoryOrderRepository.cs) — `List`

```csharp
public ActionResult<IEnumerable<Order>> ListOrders(
    [FromQuery] OrderStatus? status = null,
    [FromQuery][Range(1, 100)] int limit = 20,
    [FromQuery][Range(0, int.MaxValue)] int offset = 0)
```

**Verdt å stoppe opp ved:**

**Default-verdiene havner i spesifikasjonen.** `"default": 20` står i
`/openapi/v1.json`, og Scalar fyller dem inn for deg. Det er code-first når det er på
sitt beste: én kilde, ingen synk.

**`[AsParameters]` er Minimal API-mekanismen, ikke controller-mekanismen.**
Oppgaveteksten foreslår `[AsParameters] OrderQuery query` med en
`record OrderQuery(OrderStatus? Status, int Limit = 20, int Offset = 0)`, og det er
riktig råd — **for et Minimal API**. I en controller binder MVC en record via
`[FromQuery]` uten å ta med konstruktørens default-verdier, så `?limit` som mangler blir
`0` i stedet for `20`. Vanlige parametere med default-verdier binder forutsigbart.
Nok en liten forskjell mellom de to stilene som ikke står i noen oppsummering.

**`[Range(1, 100)]` er ikke pynt.** Uten en øvre grense er `?limit=1000000` en gratis
måte å belaste serveren på. At grensen står som en annotasjon betyr at den også er
*dokumentert* — en klient kan lese seg til den i stedet for å finne den ved å treffe en
500.

**Sorteringen er en beslutning som ikke står noe sted i kontrakten.** «Nyeste først» er
implementert i repositoryet og beskrevet i `[EndpointDescription]`, men det finnes ingen
maskinlesbar måte å uttrykke det på i OpenAPI. Paginering uten en definert og *stabil*
sortering er meningsløs — du kan få samme ordre på side 1 og side 2. Det er en av de
tingene en spesifikasjon ikke fanger, og som derfor må stå i prosa og i en test.

---

## Oppgave 9: Polymorfisme (Ekspertnivå)

**Løst, men i et eget prosjekt:** [`Oppgave9/`](Oppgave9/).

📄 [`Oppgave9/Models.cs`](Oppgave9/Models.cs)
📄 [`Oppgave9/NOTAT.md`](Oppgave9/NOTAT.md) — hva `/openapi/v1.json` faktisk ble
📄 [`Oppgave9/SelfTest.cs`](Oppgave9/SelfTest.cs)

```bash
dotnet run --project Oppgave9 -- --selftest   # serialisering + deserialisering
dotnet run --project Oppgave9                 # API på 5036, /openapi/v1.json + /scalar/v1
```

**Hvorfor ikke i hovedmodellen:** `[JsonPolymorphic]` i `Order` endrer formen på
ordre-objektet og brekker wire-kompatibiliteten med `4-Frontend/kaffebar-api`.
Referanse-implementasjonen har utelatt oppgave 9 av samme grunn.

**Kortversjonen** (hele historien i `NOTAT.md`):

- Serialisering og deserialisering virker med to attributter og null kode. En ukjent
  diskriminator gir `JsonException` — den blir ikke stille ignorert.
- **Men du får `anyOf`, ikke `oneOf`.** Spesifikasjonen er gyldig, men semantisk
  løsere enn den håndskrevne kontrakten i Java-sporet, som sier `oneOf`.
- Skjemanavnene blir `OrderItemCoffeeItem` og `OrderItemPastryItem`, ikke `CoffeeItem`
  og `PastryItem`.
- `type` er `required` på basetypen men *valgfri* i undertypene, så den genererte
  TypeScript-typen blir en union du **ikke** kan narrowe på `item.type`.
- Det overraskende: den vanskelige delen (Jackson-/STJ-oppsettet) var gratis. Den
  enkle delen (å beskrive det presist) var det ikke.

---

## Oppgave 10: Autentisering med Microsoft Entra ID (Ekspertnivå)

**Løsningen:** ferdig skrevet, **av som standard**, og verifisert lokalt uten tenant.

📄 [`Auth/AuthPolicies.cs`](Kaffebar/Auth/AuthPolicies.cs)
📄 [`OpenApi/SecuritySchemeTransformer.cs`](Kaffebar/OpenApi/SecuritySchemeTransformer.cs)
📄 [`appsettings.json`](Kaffebar/appsettings.json) — `Kaffebar:Auth` og `AzureAd`

> **Autentisering er AV som standard, og det er et bevisst valg.** Er den på, kan ikke
> frontenden i samling 4 snakke med API-et uten at du bygger innlogging inn der også, og
> bonussteget der brekker.

### Hva som er beskyttet, og hvorfor

| Endepunkt | Tilgang | Begrunnelse |
| --- | --- | --- |
| `GET /menu` | Alle | En meny er offentlig informasjon. Krever du innlogging for å se prisen på en kaffe, har du bygget feil produkt. |
| `POST /orders` | Alle | Kunden bestiller. Skal hen først lage en konto, mister du kunden. |
| `GET /orders/{id}` | Alle i fasiten | Diskusjonsverdig: id-en er en uuid, altså ikke gjettbar, men den er heller ikke hemmelig. I et ekte system ville denne enten krevd en kvitteringskode eller vært tilgjengelig for eieren. |
| `PATCH /orders/{id}` | **Barista** | Å flytte andres bestillinger til «ferdig» er en ansatt-handling. |
| `POST /orders/{id}/status` | **Barista** | Samme operasjon, samme krav. |

Skillet følger rollene i oppgaveteksten: *kunde* leser og bestiller, *barista* endrer
tilstand.

### Hvordan det er skrudd sammen

Ett attributt på endepunktene, og konfigurasjonen avgjør hva det betyr:

```csharp
[Authorize(Policy = AuthPolicies.Barista)]
```

Er `Kaffebar:Auth:Enabled` false, registreres policyen som `RequireAssertion(_ => true)`
— en gjennomkjøring. Ingen `#if`, ingen to sett med attributter, ingen kode som bare
finnes i én konfigurasjon.

`Kaffebar:Auth:Mode` velger hvordan tokens valideres:

- **`EntraId`** — `AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))`.
  Den ekte varen.
- **`LocalJwt`** — vanlig JWT Bearer mot nøkkelen `dotnet user-jwts` lager. Uten en
  tenant er dette den eneste måten å faktisk *verifisere* 401/200-oppførselen.

### Verifisert lokalt, uten tenant

```bash
cd Kaffebar
TOKEN=$(dotnet user-jwts create --name barista --role Barista -o token)

Kaffebar__Auth__Enabled=true \
Kaffebar__Auth__Mode=LocalJwt \
Kaffebar__Auth__RequireBaristaRole=true \
dotnet run
```

| Kall | Svar |
| --- | --- |
| `GET /menu` uten token | **200** |
| `POST /orders` uten token | **201** |
| `PATCH /orders/{id}` uten token | **401**, `application/problem+json` |
| `PATCH /orders/{id}` med Barista-token | **200** |
| `PATCH /orders/{id}` med token uten rollen | **403** |

Det er så langt man kommer uten tenant, og det er godt nok: det som testes er
autorisasjonen din, ikke Microsofts token-utsteder.

**En felle underveis:** setter du `RoleClaimType = "role"` i
`TokenValidationParameters`, får du **403 med et token som er helt riktig**.
JWT-handleren mapper som standard `role` (og Entra sin `roles`) til `ClaimTypes.Role`,
og det er der `RequireRole("Barista")` leter. Overstyrer du, peker du autorisasjonen på
et claim mappingen nettopp har døpt om. Vi setter den derfor bevisst ikke.

### Hva du faktisk må gjøre i Entra ID

Repoet inneholder **ingen** tenant-id-er, client-id-er eller hemmeligheter — bare
plassholdere i `AzureAd`-seksjonen. For å kjøre mot en ekte tenant:

1. **App-registrering** i Entra ID → noter `Directory (tenant) ID` og
   `Application (client) ID`.
2. **Expose an API** → sett App ID URI til `api://<client-id>` og legg til et scope, for
   eksempel `access`. Det er dette som blir `Audience`.
3. **App roles** → lag rollen `Barista` (Allowed member types: Users/Groups). Tildel den
   til de som skal være baristaer under *Enterprise applications → Users and groups*.
4. Fyll inn `AzureAd`-seksjonen og sett `Kaffebar:Auth:Enabled = true`,
   `Mode = EntraId`, `RequireBaristaRole = true`.
5. Åpne Scalar, trykk **Authorize**, lim inn et access token. `securitySchemes` ligger
   allerede i spesifikasjonen — se `SecuritySchemeTransformer`.

**Legg client-id-en i user secrets eller en miljøvariabel, ikke i `appsettings.json`.**
Den er ikke hemmelig i kryptografisk forstand, men den er miljøspesifikk, og en
`appsettings.json` med en ekte tenant sjekket inn er starten på en dårlig vane.

### Og hvorfor spesifikasjonen trenger en transformer

`IOpenApiDocumentTransformer` legger `securitySchemes` inn i `/openapi/v1.json` slik at
Scalar viser et «Authorize»-felt. Det er verdt å merke seg som et generelt poeng:
rammeverket utleder alt det kan fra koden, men *hvordan man får tak i et token* er ikke
utledbart. Da skriver du det selv.

---

## Code-first vs contract-first — hva du vinner og taper

Dette er den nyttigste samtalen å ta når begge sporene er i samme rom.

### Hva code-first gir deg som contract-first ikke gjør

- **Ingen kodegenerator i byggeløypa.** Ingen `target/generated-sources`, ingen
  IDE som ikke finner `Order` før du har bygget én gang, ingen generatorversjon å
  krangle med. Du skriver C# og trykker F5.
- **Ett sted å endre.** Legger du til et felt, er du ferdig. I Java-sporet endrer du
  YAML, bygger, og implementerer.
- **Refaktorering virker.** Døper du om en egenskap i Rider eller VS Code, følger
  spesifikasjonen med. Døper du om et felt i en YAML, følger ingenting med.
- **Typene er ekte C#-typer fra dag én.** Ingen oversettelse, ingen overraskelser om
  at `number` ble `BigDecimal`.

### Hva contract-first gir deg som code-first ikke gjør

- **Spesifikasjonen er en beslutning, ikke en utledning.** `oneOf` betyr `oneOf` fordi
  noen skrev det. Sammenlign med `anyOf`-en i oppgave 9.
- **Kontrakten finnes før koden.** To team kan bli enige om den, og begge kan begynne
  samtidig. Her må API-et eksistere først.
- **Brekkende endringer er synlige i diffen.** En `git diff` på en YAML viser at
  `required` fikk ett felt til. En `git diff` på en C#-record der noen fjernet et
  spørsmålstegn viser ikke at API-kontrakten endret seg — men det gjorde den.
- **Én sannhet, ikke to.** Hele `Kaffebar/OpenApi/`-mappa — tre transformere — finnes fordi
  den utledede spesifikasjonen ikke traff. Den mappa har ingen motpart i Java-prosjektet.

### Hva som faktisk skilte i praksis

Da de tre implementasjonene ble stilt opp mot den samme frontenden, var dette det som
måtte fikses på .NET-siden:

| Problem | Fiks |
| --- | --- |
| `errors` var en ordbok, ikke en liste | `InvalidModelStateResponseFactory` |
| `ProblemDetails` het ikke `Problem`, som kontrakten | `CreateSchemaReferenceId` |
| `errors` manglet i spesifikasjonen | `ProblemDetailsTransformer` |
| `createdAt` ble `unknown` i TypeScript (egen JsonConverter) | `ContractSchemaTransformer` |
| `null` snek seg inn i `MilkType`-enumen | `ContractSchemaTransformer` |
| Valgfrie felt ble `T \| null` i stedet for `T?` | `ContractSchemaTransformer` |
| `quantity` ble `integer \| string` | `NumberHandling = Strict` |

Ingen av dem endret én eneste byte på tråden. Alle endret hva spesifikasjonen *påstod*
om tråden — og det er den påstanden andre genererer klienter fra.

**Konklusjonen er ikke at det ene er bedre.** Den er: *for et API andre skal generere
klienter fra, må du lese spesifikasjonen du produserer like nøye som koden.* I
contract-first er det uunngåelig. I code-first er det noe du må huske.

---

## De vanligste feilene, og hva symptomet er

| Symptom | Årsak | Fiks |
| --- | --- | --- |
| `"status": 0` i stedet for `"PENDING"` | `JsonStringEnumConverter` mangler | Registrer den — og husk **begge** stedene |
| Enums er strenger fra controllere, tall fra Minimal API | Bare MVC sine `JsonOptions` er satt | `ConfigureHttpJsonOptions` også |
| 500 ved første POST, «validation metadata must be associated with the constructor parameter» | `[property: …]` på en posisjonell record | Bruk `init`-egenskaper |
| Valideringen virker, men `minLength`/`pattern` mangler i spesifikasjonen | Attributtene står på konstruktør-parametere | Bruk `init`-egenskaper |
| `/orders/ikke-en-guid` gir 404 og du ventet 400 | `:guid`-constrainten gjør at ruten ikke matcher | Bevisst valg — se oppgave 5 |
| Feilsvar har riktig body men `Content-Type: application/json` | `[Produces("application/json")]` på controlleren | Fjern det |
| Frontenden krasjer på `problem.errors.map is not a function` | `ValidationProblemDetails` bruker en ordbok | `InvalidModelStateResponseFactory` |
| `"milkType": null` i svaret | `DefaultIgnoreCondition` ikke satt | `JsonIgnoreCondition.WhenWritingNull` |
| `quantity` ble `number \| string` i genererte typer | `JsonSerializerDefaults.Web` tillater tall som streng | `NumberHandling = JsonNumberHandling.Strict` |
| `createdAt: unknown` i genererte typer | Egen `JsonConverter` skjuler typen for skjemageneratoren | Skjema-transformer som setter `string`/`date-time` |
| 403 med et token som ser riktig ut | `RoleClaimType` overstyrt til `"role"` | La den stå — mappingen gjør jobben |
| `?limit` som mangler gir 0 treff | En record bundet med `[FromQuery]` mister default-verdier | Vanlige parametere med default-verdier |

---

## Ligger du etter? Slik tar du fasiten i bruk

Fasiten er ikke juks. Den er en tidsboks — kom deg videre til neste oppgave.

**Hopp over én oppgave.** Kopier den aktuelle filen inn i ditt eget prosjekt. Modellene
(`Models/`), lagringen (`Storage/`) og feilhåndteringen (`Errors/`) er nesten
frittstående, og de har ingen avhengigheter til hverandre utover `Models`.

**Start på nytt fra et kjent punkt.** Kopier hele `Kaffebar/`-mappa. Husk at fasiten
også kjører på 5035, så du kan ikke ha begge oppe samtidig.

**Bare se hvordan det ser ut.** Åpne begge prosjektene side om side og diff
`Program.cs`. Det er den raskeste veien til «å, det var sånn».

**Til samling 4:** har du ditt eget API som svarer likt som dette, kan du peke
frontenden på det:

```bash
cd 4-Frontend/kaffebar-web
KAFFEBAR_API_URL=http://localhost:5035 yarn dev
```

Kravet er ikke at koden er lik. Kravet er at stier, feltnavn, enum-verdier, statuskoder
og media types er like. Det er det kontrakt-kompatibilitet betyr — og det er testet:
hele Next-appen typesjekker mot typer generert fra dette API-et, og både menyen,
bestillingsskjemaet og baristakøen virker mot det.

---

## Valgfritt siste steg: hendelser til RabbitMQ

**Dette er ikke en del av oppgavesettet.** Oppgavene i samling 3 sier ingenting om
hendelser.

Det er likevel med, av én grunn: peker du frontenden i samling 4 på ditt eget API, får
du bare steg 1 (data over HTTP), ikke steg 2 (to vinduer som oppdaterer seg selv).

📄 [`Events/OrderEventPublisher.cs`](Kaffebar/Events/OrderEventPublisher.cs)
📄 [`Events/AutoBarista.cs`](Kaffebar/Events/AutoBarista.cs)

**Det er AV som standard.** Er flagget av, gjør publiseringen bokstavelig talt
ingenting. `GET /health` rapporterer `"rabbitmq": "disabled"`.

```bash
# RabbitMQ må kjøre: docker compose up rabbitmq  (fra rota av repoet)
cd Kaffebar
Kaffebar__Events__Enabled=true Kaffebar__Barista__AutoEnabled=true dotnet run
```

| Innstilling | Default | Hva |
| --- | --- | --- |
| `Kaffebar:Events:Enabled` | `false` | Publiser `order.created` og `order.status-changed` |
| `Kaffebar:Events:Exchange` | `kaffebar` | Topic exchange, durable |
| `Kaffebar:Events:Uri` | `amqp://guest:guest@localhost:5672` | Broker |
| `Kaffebar:Barista:AutoEnabled` | `false` | Flytt ordrer `PENDING → BREWING → READY` av seg selv |
| `Kaffebar:Barista:IntervalMs` | `8000` | Hvor ofte auto-baristaen jobber |

Samme exchange, samme routing keys og samme meldingsformat som
`4-Frontend/kaffebar-api`:

```jsonc
{
  "type": "order.status-changed",
  "timestamp": "2026-09-13T13:53:14.314Z",
  "data": { /* hele Order-objektet etter endringen */ },
  "previousStatus": "PENDING"
}
```

**To detaljer det er verdt å kopiere:**

Publiseringen skjer i bakgrunnen, og alle feil fanges. En broker som er nede skal aldri
gjøre et HTTP-kall som ellers gikk bra om til en 500 — og den skal aldri få kunden til å
vente. Hendelser er en bonus; bestillingen er jobben. (Testet: med broker stoppet svarer
`POST /orders` fortsatt 201, `/health` sier `disconnected`, og publiseringen tar seg opp
igjen av seg selv når broker kommer tilbake.)

Exchangen deklareres hver gang en ny kanal lages, ikke én gang ved oppstart. Da er den
garantert der før den første meldingen — også etter en reconnect.
