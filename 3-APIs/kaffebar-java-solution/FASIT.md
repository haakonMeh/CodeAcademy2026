# Fasit — Kaffebar, Java-sporet (samling 3)

Dette er løsningen på oppgave 1 til 9 i
[`../kaffebar-java/oppgave.md`](../kaffebar-java/oppgave.md), som et komplett,
kjørbart Spring Boot-prosjekt.

Dokumentet følger **oppgavene**, ikke koden. Én seksjon per oppgave, i samme
rekkefølge som oppgave.md, slik at det kan brukes som manus når vi går gjennom
løsningen på storskjerm.

```bash
cd 3-APIs/kaffebar-java-solution
mvn clean verify        # bygger, genererer og kjører 20 tester
mvn spring-boot:run     # http://localhost:8080
```

| URL | Hva |
| --- | --- |
| http://localhost:8080/menu | Menyen |
| http://localhost:8080/swagger-ui.html | Swagger-UI |
| http://localhost:8080/v3/api-docs | Spesifikasjonen springdoc bygger av den kjørende koden |
| http://localhost:8080/openapi.json | Samme fil, på stien `kaffebar-web` forventer |

**Starteren er urørt.** `3-APIs/kaffebar-java/` står nøyaktig som du klonet den.
Alt her ligger i en søskenmappe, så du kan ha begge åpne samtidig i IDE-et og diffe
dem mot hverandre.

---

## Det store bildet først

Hele poenget med Java-sporet er denne ene setningen:

> Kontrakten er kilden. Koden er et resultat.

Det merkes best på hvor lite som står i `OrdersController`. Ruting, media types,
binding av query-parametere med default-verdier, hele valideringen og all
API-dokumentasjonen kommer fra `kaffebar-api.yaml` via det genererte interfacet. Det
som er igjen i kontrolleren er forretningslogikken — og den er omtrent 60 linjer.

Legg også merke til hva du *ikke* finner: ingen `@GetMapping`, ingen `@RequestParam`,
ingen `@Valid` skrevet for hånd. Skriver du en av dem, har du sannsynligvis
implementert noe kontrakten allerede sa.

### Filene, kort

```
kaffebar-java-solution/
├── src/main/resources/api/kaffebar-api.yaml   # KONTRAKTEN. Alt starter her.
├── src/main/java/no/soprasteria/kaffebar/
│   ├── controller/     MenuController, OrdersController, HealthController
│   ├── store/          OrderStore (ConcurrentHashMap), Menu, SeedOrders
│   ├── error/          ApiExceptionHandler (RFC 7807), NotFoundException
│   ├── config/         OpenApiConfiguration
│   └── events/         valgfritt: RabbitMQ + auto-barista (AV som standard)
├── src/test/java/…/KaffebarApiTest.java       # 20 MockMvc-tester
├── oppgave-9/          # polymorfisme, egen kontrakt + NOTAT.md
└── FASIT.md            # denne
```

### Hva som er lagt til utover starteren, og hvorfor

| Avhengighet | Hvorfor |
| --- | --- |
| `spring-boot-starter-test` | Testene. Var ikke i starteren. |
| `spring-boot-webmvc-test` | Spring Boot 4 har delt test-autokonfigurasjonen opp per teknologi. `@AutoConfigureMockMvc` bor her nå, og følger ikke lenger med `spring-boot-starter-test`. |
| `spring-boot-starter-amqp` | Kun for det valgfrie siste steget (hendelser til RabbitMQ), som er **av** som standard. Se nederst. |

Alt annet er identisk med starteren: Java 21, Spring Boot 4.0.2,
`openapi-generator-maven-plugin` 7.4.0 med `generatorName spring`, `interfaceOnly`,
`useSpringBoot3`, `skipDefaultInterface`, `dateLibrary java8`. `artifactId` og `name`
er endret så prosjektene kan ligge åpne samtidig.

---

## Oppgave 1: Den første bestillingen (POST)

**Løsningen:** `POST /orders` i `paths`, med `CreateOrderRequest` inn og `Order` ut.

📄 [`src/main/resources/api/kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `paths./orders.post`
📄 [`controller/OrdersController.java`](src/main/java/no/soprasteria/kaffebar/controller/OrdersController.java) — `createOrder`

```yaml
post:
  operationId: createOrder
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/CreateOrderRequest'
  responses:
    '201':
      headers:
        Location: { schema: { type: string } }
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Order'
```

**Verdt å stoppe opp ved:**

**To typer, ikke én.** `CreateOrderRequest` og `Order` er ulike skjemaer, og det er
hele poenget med oppgaven. Klienten sender `coffeeId`; serveren svarer med `orderId`,
`coffeeName`, `status`, `createdAt` og `updatedAt` — felter klienten verken kan eller
skal sette. Hadde vi brukt `Order` begge veier, måtte alle de feltene vært valgfrie, og
kontrakten ville sluttet å si noe presist om noe som helst.

**`201`, ikke `200`.** Og `Location`-headeren er ikke pynt: den forteller klienten hvor
ressursen bor nå. `ResponseEntity.created(URI.create("/orders/" + id))` gjør begge deler.

**`coffeeName` er denormalisert med vilje.** Ordren bærer med seg navnet på kaffen, ikke
bare id-en. Det er et bevisst brudd på «ikke dupliser data»: uten det må frontenden
hente `/menu` for å vise én ordre, og en kø med tjue ordrer blir tjue ekstra kall eller
en klient som må vedlikeholde et oppslag. Prisen er at ordren ikke endrer navn hvis
menyen gjør det — noe som for en kvittering er riktig oppførsel uansett.

---

## Oppgave 2: Bygg og implementer

**Løsningen:** `mvn clean compile`, se på `target/generated-sources/`, og implementer
det genererte interfacet.

📄 [`controller/OrdersController.java`](src/main/java/no/soprasteria/kaffebar/controller/OrdersController.java)
📄 [`controller/MenuController.java`](src/main/java/no/soprasteria/kaffebar/controller/MenuController.java)

Generatoren lager tre interfacer og ti modeller ut av kontrakten:

```
target/generated-sources/openapi/src/main/java/no/soprasteria/kaffebar/
├── api/     MenuApi, OrdersApi, HealthApi, ApiUtil
└── model/   Coffee, Order, OrderStatus, CoffeeSize, MilkType,
            CreateOrderRequest, UpdateOrderStatusRequest,
            Problem, ValidationError, Health
```

Én kontrollerklasse per interface, akkurat som `MenuController` allerede gjorde det.

### Diskusjon: fikk metodene logiske navn?

**Ja — fordi vi ga dem navn.** Metodenavnet kommer rett fra `operationId` i YAML-en:

| `operationId` | Metode i det genererte interfacet |
| --- | --- |
| `getMenu` | `ResponseEntity<List<Coffee>> getMenu()` |
| `createOrder` | `ResponseEntity<Order> createOrder(CreateOrderRequest)` |
| `getOrder` | `ResponseEntity<Order> getOrder(UUID orderId)` |
| `listOrders` | `ResponseEntity<List<Order>> listOrders(OrderStatus, Integer, Integer)` |
| `updateOrderStatus` | `ResponseEntity<Order> updateOrderStatus(UUID, UpdateOrderStatusRequest)` |
| `setOrderStatus` | `ResponseEntity<Order> setOrderStatus(UUID, UpdateOrderStatusRequest)` |

**Sløyfer du `operationId`, finner generatoren på et navn selv** — typisk
`ordersOrderIdPatch`. Det kompilerer, men er ikke noe du vil lese om seks måneder, og
det endrer seg hvis du flytter endepunktet. `operationId` er ikke valgfritt i praksis.
Det er navnet på metoden din.

Navnet på *interfacet* kommer derimot ikke fra `tags` — det kommer fra første
path-segment. `/orders`, `/orders/{orderId}` og `/orders/{orderId}/status` havner alle
i `OrdersApi`. (Vil du at `tags` skal styre det, setter du `useTags=true` i
`configOptions`. Vi har latt det stå som i starteren.)

### Diskusjon: hvordan ser modellene ut i koden?

Vanlige POJO-er med Jackson- og Bean Validation-annotasjoner, ikke `record`-er:

```java
public class CreateOrderRequest {
  private UUID coffeeId;
  private String customerName;
  private CoffeeSize size;
  private MilkType milkType;
  private Boolean extraShot;
  private Integer quantity = 1;      // <- default-verdien fra kontrakten

  @NotNull @Valid
  @JsonProperty("coffeeId")
  public UUID getCoffeeId() { ... }

  @NotNull @Pattern(regexp = "^(?!\\s*$).+$") @Size(min = 2, max = 50)
  @JsonProperty("customerName")
  public String getCustomerName() { ... }
}
```

Fire ting å peke på:

- **Typene er ekte.** `format: uuid` ble `java.util.UUID`, ikke `String`.
  `format: date-time` ble `OffsetDateTime` (det er `dateLibrary java8` som gjør det).
  `type: number` uten format ble `BigDecimal`.
- **Enum-skjemaene ble egne Java-enumer** med `@JsonValue`/`@JsonCreator`, fordi vi
  definerte dem som navngitte skjemaer under `components`. Hadde vi skrevet
  `enum: [...]` rett inne i `Order`, hadde vi fått en indre `SizeEnum` i `Order` og en
  til i `CreateOrderRequest` — to typer for samme begrep.
- **Default-verdien fra kontrakten er et felt-initialiserende uttrykk**
  (`= 1`). Ingen kode i kontrolleren fyller inn `quantity`.
- **Konstruktøren tar bare de påkrevde feltene.** Valgfrie felt settes med settere
  eller fluent-metoder. Det er praktisk i `OrderStore.create(...)`.

**Ikke rediger de genererte filene.** `target/` slettes ved neste `mvn clean`. Vil du
endre noe der, endrer du YAML-en.

---

## Oppgave 3: Typer, Enums og Valgfrihet

**Løsningen:** `CoffeeSize`, `MilkType` og `OrderStatus` som egne skjemaer under
`components/schemas`, og `required`-lista styrer hva som må være med.

📄 [`kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `components.schemas.CoffeeSize`, `MilkType`, `OrderStatus`

```yaml
CoffeeSize:
  type: string
  enum: [SMALL, MEDIUM, LARGE]

CreateOrderRequest:
  required: [coffeeId, customerName, size]   # <- merk: ikke milkType, ikke extraShot
  properties:
    size:
      $ref: '#/components/schemas/CoffeeSize'
    milkType:
      $ref: '#/components/schemas/MilkType'
    extraShot:
      type: boolean
```

**Verdt å stoppe opp ved:**

**Egne skjemaer, ikke inline enums.** `$ref` gjør at `size` i `CreateOrderRequest` og
`size` i `Order` er *den samme typen*. Skriver du enumen inline begge steder, får du to
uavhengige Java-enumer med samme verdier, og kompilatoren vil ikke la deg sende den ene
der den andre forventes. Dette er den vanligste tabben i oppgave 3.

**`required` er den eneste bryteren.** Et felt er valgfritt med mindre det står i
`required`. Det finnes ingen `optional: true`. Og legg merke til at `required` ligger på
objektet, ikke på feltet — det er ofte overraskende første gang.

**Valgfritt er ikke det samme som nullable.** `milkType` kan være *fraværende*. Den kan
ikke være `null`. I JSON er det to forskjellige ting, og vi har valgt at fraværende er
det riktige — `spring.jackson.default-property-inclusion=non_null` i
`application.properties` sørger for at serveren svarer likt: en ordre uten melk har
ingen `milkType`-nøkkel i det hele tatt. Det er også slik referanse-implementasjonen
oppfører seg, og det gjør de tre implementasjonene diffbare.

**`Boolean`, ikke `boolean`.** Generatoren bruker wrapper-typen for valgfrie felt,
nettopp fordi den trenger `null` for å kunne skille «ikke sendt» fra `false`.

---

## Oppgave 4: Validering i kontrakten

**Løsningen:** `minLength`, `maxLength`, `pattern`, `minimum` og `maximum` i YAML-en.
Ingen `if`-setninger noe sted i koden.

📄 [`kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `CreateOrderRequest`
📄 [`error/ApiExceptionHandler.java`](src/main/java/no/soprasteria/kaffebar/error/ApiExceptionHandler.java)

Dette er læringspunktet i oppgaven, så her er hele oversettelsen:

| YAML | Generert annotasjon | Kaster ved brudd |
| --- | --- | --- |
| `required: [coffeeId]` | `@NotNull` | `MethodArgumentNotValidException` |
| `minLength: 2` / `maxLength: 50` | `@Size(min = 2, max = 50)` | `MethodArgumentNotValidException` |
| `pattern: '^(?!\s*$).+$'` | `@Pattern(regexp = "^(?!\\s*$).+$")` | `MethodArgumentNotValidException` |
| `minimum: 1` / `maximum: 10` | `@Min(1)` / `@Max(10)` | `MethodArgumentNotValidException` |
| `minimum`/`maximum` på en **query-parameter** | `@Min(1) @Max(100)` på metodeparameteret | `ConstraintViolationException` |
| `enum: [...]` | egen Java-enum med `@JsonCreator` | `HttpMessageNotReadableException` |
| `format: uuid` i path | `UUID`-parameter | `MethodArgumentTypeMismatchException` |

Verifiser det selv: `target/generated-sources/.../model/CreateOrderRequest.java`.

**Verdt å stoppe opp ved — de fire feilene kommer langs fire ulike veier.**

Det ser ut som én ting («ugyldig input»), men rammeverket behandler det som fire, og
alle fire må håndteres for at API-et skal oppføre seg konsistent:

1. **Bean Validation på bodyen.** Objektet bygges, og *så* valideres det.
   `@Valid @RequestBody` → `MethodArgumentNotValidException` med en `BindingResult` som
   inneholder felt og melding. Dette er den lette.
2. **Bean Validation på metodeparametere.** Det genererte interfacet er annotert med
   `@Validated`, så Spring legger en AOP-proxy rundt kontrolleren.
   `?limit=200` → `ConstraintViolationException`. **Ikke** samme exception som over.
   Uten en egen handler gir `?limit=200` en 500, og det er en av de vanligste
   overraskelsene i dette oppsettet.
3. **Ugyldig enum-verdi.** `"size": "HUGE"` stopper allerede i deserialiseringen —
   Jackson rekker aldri å lage objektet, så `@Valid` kjører aldri. Du må grave
   feltnavnet ut av Jackson-feilen selv (`JacksonException.getPath()`) for å få det inn
   i `errors`-lista.
4. **Ugyldig uuid i pathen.** `MethodArgumentTypeMismatchException` → se oppgave 5.

**Fella med `pattern`: `@Pattern` bruker `matches()`, ikke `find()`.**

Referansekontrakten i 4-Frontend bruker `pattern: '\S'` — «inneholder minst ett
ikke-blankt tegn». JSON Schema tolker mønstre som *delvis* treff, så det stemmer der.
Jakarta Bean Validation tolker `@Pattern` som *fullt* treff. `@Pattern(regexp = "\\S")`
i Java betyr derfor «nøyaktig ett ikke-blankt tegn», og `"Ada"` ville blitt avvist.

Kontrakten vår bruker `^(?!\s*$).+$`, som betyr det samme i begge verdener. Dette er et
ekte contract-first-problem: samme YAML, to forskjellige valideringsmotorer, to
forskjellige svar. Skriver du et `pattern`, må du vite hvem som skal håndheve det.

---

## Oppgave 5: Hent en spesifikk bestilling (Path Parameters)

**Løsningen:** `GET /orders/{orderId}`, med parameteret definert én gang under
`components/parameters` og `$ref`-et inn.

📄 [`kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `components.parameters.OrderId`

```yaml
/orders/{orderId}:
  parameters:
    - $ref: '#/components/parameters/OrderId'
  get:
    operationId: getOrder
```

**Verdt å stoppe opp ved:**

**Parameteret ligger på *pathen*, ikke på operasjonen.** `/orders/{orderId}` har både
`get` og `patch`, og begge trenger `orderId`. Definerer du det på path-nivå, arver begge
det. Skriver du det i hver operasjon, har du to steder som kan komme ut av synk.

**Og det ligger under `components/parameters`, ikke i pathen.** `/orders/{orderId}` og
`/orders/{orderId}/status` bruker det samme parameteret. Nøyaktig samme argument som for
`$ref` på skjemaer: én definisjon, mange brukssteder.

**`format: uuid` gir deg typesikkerhet gratis.** Parameteret blir `UUID orderId` i det
genererte interfacet, ikke `String`. Du slipper å parse, og du kan ikke glemme det.

### Hva skjer med en ugyldig uuid?

```bash
curl -i localhost:8080/orders/ikke-en-uuid
```

Spring klarer ikke å konvertere strengen til `UUID` og kaster
`MethodArgumentTypeMismatchException`. **Uten en handler for den blir dette en 500** —
og det er feil. Serveren fungerer helt fint; det er requesten som er ugyldig.

Fasiten svarer **400** med Problem Details:

```json
{
  "type": "https://codeacademy.soprasteria.no/problems/validation-error",
  "title": "Ugyldig forespørsel",
  "status": 400,
  "instance": "/orders/ikke-en-uuid",
  "errors": [{ "field": "orderId", "message": "må være en gyldig UUID" }]
}
```

> **Sammenlign med .NET-sporet:** der gir det samme kallet **404**, fordi
> route-constrainten `{orderId:guid}` gjør at ruten ikke matcher i det hele tatt.
> Ingen av dem er feil. Det er to rammeverk som har tatt hver sin beslutning om hva som
> er den mest presise feilen. Se `../kaffebar-dotnet-solution/FASIT.md`.

---

## Oppgave 6: Standardisert Feilhåndtering

**Løsningen:** ett `Problem`-skjema etter RFC 7807, `$ref`-et fra alle feilresponsene,
og én `@RestControllerAdvice` som produserer det.

📄 [`kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `components.responses.BadRequest` / `NotFound`, `components.schemas.Problem`
📄 [`error/ApiExceptionHandler.java`](src/main/java/no/soprasteria/kaffebar/error/ApiExceptionHandler.java)
📄 [`error/NotFoundException.java`](src/main/java/no/soprasteria/kaffebar/error/NotFoundException.java)

```yaml
components:
  responses:
    NotFound:
      description: Ressursen finnes ikke.
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/Problem'
```

**Verdt å stoppe opp ved:**

**`$ref` på hele responsen, ikke bare på skjemaet.** `components/responses` lar deg
gjenbruke media type, beskrivelse og skjema i én referanse. Ni steder i kontrakten
peker på `BadRequest` eller `NotFound`. Endrer du feilformatet, endrer du det ett sted.

**`application/problem+json`, ikke `application/json`.** Det er hele vitsen med RFC
7807: en klient kan kjenne igjen et feilsvar på media typen alene, uten å gjette på
formen. Spring setter den automatisk når en handler returnerer `ProblemDetail`.

**Spring har RFC 7807 innebygd.** `ProblemDetail` finnes i `org.springframework.http`.
Vi bruker den i stedet for den genererte `Problem`-klassen — den genererte klassen
beskriver svaret i kontrakten, `ProblemDetail` produserer det. `errors`-lista legges på
som en extension med `problem.setProperty("errors", …)`, noe RFC 7807 eksplisitt åpner
for.

**Én advice, ikke try/catch i kontrollerne.** `OrdersController` kaster
`NotFoundException` og er ferdig med det. Hvordan en 404 ser ut på tråden bestemmes ett
sted. Hadde hver kontroller bygget sitt eget feilsvar, ville de drevet fra hverandre
innen tredje endepunkt.

**Både `type` og `instance` er verdt å fylle ut.** `type` er en URI som identifiserer
*feiltypen* og er noe klienten kan sammenligne på. `instance` er stien som feilet, og er
det du leter etter i en logg. `title` er for mennesker. `status` dupliserer
HTTP-statusen med vilje, slik at objektet gir mening også når det er logget alene.

---

## Oppgave 7: Baristaens oppdatering (PATCH vs PUT)

**Løsningen:** `PATCH /orders/{orderId}` som anbefalt variant, og
`POST /orders/{orderId}/status` som alias. Begge tar `UpdateOrderStatusRequest`, som
bare inneholder `status`.

📄 [`kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `paths./orders/{orderId}.patch`, `paths./orders/{orderId}/status.post`
📄 [`controller/OrdersController.java`](src/main/java/no/soprasteria/kaffebar/controller/OrdersController.java) — `updateOrderStatus`, `setOrderStatus`, `applyStatus`

### Diskusjon: PUT, PATCH eller action-endepunkt?

**`PATCH` er anbefalingen.** Argumentet er kort: vi endrer *én egenskap* på en ressurs
som finnes fra før. Det er nøyaktig det `PATCH` betyr.

**`PUT` ville vært feil her**, ikke bare mindre elegant. `PUT` betyr «erstatt
ressursen med dette». Da må klienten sende hele ordren — inkludert `createdAt`,
`customerName` og `orderId` — og serveren må bestemme hva den gjør når de ikke stemmer.
Ignorere dem stille? Feile? Overskrive? Alle tre svarene er dårlige. Og en barista som
skal trykke «ferdig» har ingen grunn til å ha hele ordren i hånda.

**`POST /orders/{id}/status` er ikke galt.** Action-endepunkter er utbredt, de leses
godt, og de er det opplagte valget når operasjonen ikke er en tilstandsendring på ett
felt men en *handling* med sideeffekter — `POST /orders/{id}/refund`, for eksempel.
Prisen er at du har lagt til en ressurs (`/status`) som egentlig ikke er en ressurs, og
at du ikke lenger kan lese HTTP-metoden og vite hva som skjer.

**Hvorfor fasiten støtter begge:** i mai valgte dere ulikt. Skal frontenden i samling 4
kunne peke på deres eget API uten kodeendringer, må begge variantene finnes. Det er en
workshop-begrunnelse, ikke en API-designbegrunnelse — i et ekte API velger du én.

### Diskusjon: hvorfor et eget request-objekt?

```yaml
UpdateOrderStatusRequest:
  required: [status]
  properties:
    status:
      $ref: '#/components/schemas/OrderStatus'
```

Fordi det gjør en hel klasse feil **umulig**, i stedet for bare forbudt.

Gjenbruker du `Order` som request-type, kan en klient sende med `createdAt` eller
`customerName`, og du må skrive kode som ignorerer dem. Den koden kan glemmes. Med et
objekt som bare har `status`, finnes ikke feltene å sende — kontrakten sier det,
generatoren håndhever det, og ingen trenger å huske det.

Det er det samme argumentet som i oppgave 1: request og response er ulike ting, selv
når de handler om det samme.

---

## Oppgave 8: Filtrering og Paginering (Query Parameters)

**Løsningen:** `status`, `limit` og `offset` som query-parametere på `GET /orders`, med
default-verdier og grenser i kontrakten.

📄 [`kaffebar-api.yaml`](src/main/resources/api/kaffebar-api.yaml) — `paths./orders.get.parameters`
📄 [`store/OrderStore.java`](src/main/java/no/soprasteria/kaffebar/store/OrderStore.java) — `list(...)`

```yaml
- name: limit
  in: query
  required: false
  schema:
    type: integer
    minimum: 1
    maximum: 100
    default: 20
```

blir

```java
@Min(1) @Max(100) @Valid
@RequestParam(value = "limit", required = false, defaultValue = "20") Integer limit
```

**Verdt å stoppe opp ved:**

**`default` havner i `defaultValue` på `@RequestParam`.** Kontrolleren får aldri `null`.
Det er verdt å se på, fordi det er tredje gang i dette prosjektet at kontrakten fyller
inn noe vi ellers ville skrevet selv.

**Default hører til i `schema`, ikke ved siden av.** `default: 20` må stå inne i
`schema:`-blokken. Skriver du den på parameter-nivå, blir den ignorert uten en eneste
advarsel, og du oppdager det først når `?limit` mangler og du får alt.

**`maximum: 100` er ikke pynt.** Uten en øvre grense er `?limit=1000000` en gratis
måte å belaste serveren på. At grensen står i kontrakten betyr at den også er
*dokumentert* — en klient kan lese seg til den i stedet for å finne den ved å treffe en
500.

**Sorteringen er en kontrakt-beslutning som ikke står i kontrakten.** «Nyeste først» er
implementert i `OrderStore.list(...)` og beskrevet i `description`-feltet, men det
finnes ingen maskinlesbar måte å uttrykke det på i OpenAPI. Paginering uten en definert
og *stabil* sortering er meningsløs — du kan få samme ordre på side 1 og side 2. Dette
er en av de tingene en spesifikasjon ikke fanger, og som derfor må stå i prosa og i en
test.

---

## Oppgave 9: Polymorfisme (Ekspertnivå)

**Løst, men i en egen kontrakt:** [`oppgave-9/`](oppgave-9/).

📄 [`oppgave-9/kaffebar-bakst.yaml`](oppgave-9/kaffebar-bakst.yaml) — kontrakten
📄 [`oppgave-9/NOTAT.md`](oppgave-9/NOTAT.md) — hva generatoren faktisk produserte
📄 [`oppgave-9/src/test/java/…/PolymorfismeTest.java`](oppgave-9/src/test/java/no/soprasteria/kaffebar/bakst/PolymorfismeTest.java)

```bash
mvn -f oppgave-9/pom.xml clean test
```

**Hvorfor ikke i hovedkontrakten:** `oneOf` i `Order` endrer formen på ordre-objektet og
brekker wire-kompatibiliteten med `4-Frontend/kaffebar-api`. Den genererte
TypeScript-klienten i samling 4 måtte da skrives om.
Referanse-implementasjonen har utelatt oppgave 9 av samme grunn.

**Kortversjonen av hva som skjedde** (hele historien i `NOTAT.md`):

- `oneOf` + `discriminator` ga **et interface med to implementasjoner** —
  `interface OrderItem { String getType(); }`, og `CoffeeItem implements OrderItem`.
  Ikke arv, ikke en wrapper med to nullable felt.
- `discriminator` ble til `@JsonTypeInfo` + `@JsonSubTypes` på interfacet, og både
  serialisering og deserialisering virker uten en linje håndskrevet Jackson-kode.
- **Fiklingen:** skriver du `type: {type: string, enum: [coffee]}` i undertypene,
  kompilerer ikke resultatet. Generatoren lager `String getType()` i interfacet og
  `TypeEnum getType()` i implementasjonen. Diskriminator-feltet må stå som ren `string`.
- Og generatoren advarer ikke. Den skriver ut Java som ikke kompilerer. Bygget er
  sikkerhetsnettet — ikke YAML-en.

---

## De vanligste feilene, og hva symptomet er

| Symptom | Årsak | Fiks |
| --- | --- | --- |
| `?limit=200` gir **500** i stedet for 400 | Det genererte interfacet er `@Validated`, så det er AOP-metodevalidering som slår inn. Den kaster `ConstraintViolationException`, ikke `MethodArgumentNotValidException`. | Egen `@ExceptionHandler(ConstraintViolationException.class)` |
| `/orders/ikke-en-uuid` gir **500** | `MethodArgumentTypeMismatchException` er ikke håndtert | Egen handler → 400 |
| `"size": "HUGE"` gir 400 uten `errors`-liste | Jackson feiler før validering. `@Valid` kjører aldri. | Håndter `HttpMessageNotReadableException` og les feltnavnet fra `JacksonException.getPath()` |
| `"Ada"` avvises av `pattern: '\S'` | `@Pattern` bruker `matches()` (fullt treff), JSON Schema bruker delvis treff | `^(?!\s*$).+$` |
| Metodene heter `ordersOrderIdPatch` | `operationId` mangler | Sett `operationId` på hver operasjon |
| `size` i `Order` og i `CreateOrderRequest` er ulike typer | Enumen er skrevet inline begge steder | Eget skjema under `components/schemas` + `$ref` |
| `?limit` uten verdi gir alle ordrene | `default: 20` står på parameter-nivå, ikke i `schema:` | Flytt den inn i `schema:` |
| Svaret har `"milkType": null` | Jackson tar med null-felt som standard | `spring.jackson.default-property-inclusion=non_null` |
| Endringer i YAML-en slår ikke inn | IDE-et bygde ikke på nytt; `target/` er stale | `mvn clean compile`, og marker `target/generated-sources/openapi/src/main/java` som kildemappe |
| «Cannot find symbol: Order» i IDE-et | Generert kode ligger i `target/` og er ikke indeksert | Bygg én gang, deretter Reload/Reimport i IDE-et |
| Prisen kommer ut som `48.5` og ikke `48.50` | `BigDecimal` fra `new BigDecimal(48.50)` | Bruk `new BigDecimal("48.50")` — eller la være å bry deg; det er samme tall |
| Tidsstempler har nanosekunder | `OffsetDateTime.now()` har full presisjon | `.truncatedTo(ChronoUnit.MILLIS)` |

---

## Ligger du etter? Slik tar du fasiten i bruk

Fasiten er ikke juks. Den er en tidsboks — kom deg videre til neste oppgave.

**Hopp over én oppgave.** Kopier den aktuelle biten av
`src/main/resources/api/kaffebar-api.yaml` inn i din egen YAML, kjør
`mvn clean compile`, og implementer det nye interfacet. Kontrakten er lesbar alene;
du trenger ikke resten av prosjektet.

**Start på nytt fra et kjent punkt.** Kopier hele
`kaffebar-java-solution/src/` inn i ditt eget prosjekt og fortsett derfra. Husk å endre
`artifactId` tilbake hvis du vil kjøre på port 8080 samtidig som fasiten.

**Bare se hvordan det ser ut.** Kjør de to prosjektene side om side i IDE-et og diff
`kaffebar-api.yaml`. Det er den raskeste veien til «å, det var sånn».

**Til samling 4:** har du ditt eget API som svarer likt som dette, kan du peke
frontenden på det:

```bash
cd 4-Frontend/kaffebar-web
KAFFEBAR_API_URL=http://localhost:8080 yarn dev
```

Kravet er ikke at koden er lik. Kravet er at stier, feltnavn, enum-verdier, statuskoder
og media types er like. Det er det kontrakt-kompatibilitet betyr.

---

## Valgfritt siste steg: hendelser til RabbitMQ

**Dette er ikke en del av oppgavesettet.** Oppgavene i samling 3 sier ingenting om
hendelser, og ingen har gjort dette i mai.

Det er likevel med, av én grunn: peker du frontenden i samling 4 på ditt eget API, får
du bare steg 1 (data over HTTP), ikke steg 2 (to vinduer som oppdaterer seg selv).
Uten hendelser mangler halve poenget med samlingen.

📄 [`events/OrderEventPublisher.java`](src/main/java/no/soprasteria/kaffebar/events/OrderEventPublisher.java)
📄 [`events/AutoBarista.java`](src/main/java/no/soprasteria/kaffebar/events/AutoBarista.java)
📄 [`events/KaffebarProperties.java`](src/main/java/no/soprasteria/kaffebar/events/KaffebarProperties.java)

**Det er AV som standard.** Er flagget av, gjør publiseringen bokstavelig talt
ingenting: ingen tilkobling, ingen logglinjer, ingen endret HTTP-oppførsel.
`GET /health` rapporterer `"rabbitmq": "disabled"`.

```bash
# RabbitMQ må kjøre: docker compose up rabbitmq  (fra rota av repoet)
mvn spring-boot:run \
  -Dspring-boot.run.arguments="--kaffebar.events.enabled=true --kaffebar.barista.auto-enabled=true"
```

| Innstilling | Default | Hva |
| --- | --- | --- |
| `kaffebar.events.enabled` | `false` | Publiser `order.created` og `order.status-changed` |
| `kaffebar.events.exchange` | `kaffebar` | Topic exchange, durable |
| `kaffebar.barista.auto-enabled` | `false` | Flytt ordrer `PENDING → BREWING → READY` av seg selv |
| `kaffebar.barista.interval-ms` | `8000` | Hvor ofte auto-baristaen jobber |

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

**En detalj det er verdt å kopiere:** en broker som er nede skal aldri gjøre et
HTTP-kall som ellers gikk bra om til en 500. `publish(...)` fanger alt, logger en
advarsel og går videre. Hendelser er en bonus; bestillingen er jobben.
