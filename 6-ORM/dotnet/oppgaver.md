# Kaffebaren: Et dypdykk i ORM og Entity Framework Core

Velkommen til kveldens workshop! I dag skal vi utforske de mørkere krokene av objekt-relasjonell
mapping (ORM). Vi har strippet vekk ASP.NET Core, dependency injection og hosting. Her er det kun
deg, vanilla C#, EF Core 10 og en ekte Postgres som Testcontainers starter for deg.

Kaffebaren er den samme som i samling 3: samme meny (Filterkaffe, Espresso, Cortado …), og
de samme kundene står fortsatt i kø (Ada, Kari, Jonas og Ingrid). Men nå er den vokst opp. En
bestilling kan ha flere linjer, og vi vet hvilke bønner baristaen brukte og hvem som brente dem:

```
Order ──< OrderLine >── Bean >── Roastery
  │
  └──< StatusChange          (Pending → Brewing → Ready → PickedUp, med tidspunkt)
```

Databasen er fylt med 6 brennerier, 15 bønner og 100 ordrer med 250 linjer, spredt over et halvt år.
Hver entitet, tabell og kunde er beskrevet i [Kaffebaren: domenet og dataene](#kaffebaren-domenet-og-dataene).

### Velg din sti
Oppgavene er delt inn i fire nivåer. For å fylle kvelden (ca. 3 timer koding), velger du det
sporet som passer din erfaring best:
* **Fersk på EF Core?** Ta Nivå 1 og 2. Ferdig før tiden? Fortsett med nivå 3.
* **Erfaren, men rusten på detaljene?** Ta Nivå 2 og 3.
* **ORM-veteran?** Hopp rett til Nivå 3 og 4.

Alle bør gjøre **Forarbeidet** først. Det tar et kvarter, og det er der du får se SQL-en.

Omtrentlig tid per nivå, inkludert refleksjon i par: nivå 1 ca. 45 min, nivå 2 ca. 90 min, nivå 3 ca.
75 min og nivå 4 ca. 90 min. Oppgaver merket *bonus* er valgfrie. Ta dem hvis du har tid til overs.
Er du ferdig før tiden, finnes det også [valgfrie oppgaver](#valgfrie-oppgaver) som ikke hører til noe nivå.

Hvert nivå ligger i sin egen mappe (`nivaa-1`, `nivaa-2`, …) og er et selvstendig .NET-prosjekt. Mappen
for et nivå inneholder løsningen på nivået før, så du kan starte hvor du vil.

**Når du er ferdig med et nivå, går du videre i neste mappe.** Der er forrige nivå allerede løst, med
fasitens kode i stedet for din.

Hver oppgave har en ID (1A, 1B, 2A, 2B, 2C, 3A, 3B, 3C, 3D, 4A, 4B, 4C), en egen fil i
`Kaffebar.Orm/KaffebarRepository.Level<ID>.cs` (1B ligger i `Data/KaffebarDb.cs`) og egne tester. Innenfor et nivå kan du fint gjøre dem i
en annen rekkefølge.

> 🧭 **Hvis du står fast:** Spør kursholderen. Står du fast på et helt nivå, kan du gå videre i neste
> mappe, for der er det løst. Så kommer du deg videre til det som er interessant.

---

## Litt teori før vi starter

Du har sikkert hørt dette i teoridelen allerede. Her er det du trenger for kvelden, samlet på ett
sted.

**Den objekt-relasjonelle mismatchen.** Koden tenker i objekter som *peker på* hverandre
(`order.Lines[0].Bean.Roastery`). Databasen tenker i tabeller med rader som *refererer til*
hverandre via fremmednøkler, og i mengder du `JOIN`-er. Objekter har identitet (`==` på
referansen), arv og samlinger. Rader har primærnøkler og ingen av delene. En ORM oversetter
mellom de to verdenene. Oversettelsen er aldri gratis, og nesten alle ORM-feil er et sted der
den tapte informasjon eller gjorde noe du ikke ba om.

**Hva en ORM gjør for deg:**
- **Mapping:** klasse ↔ tabell, egenskap ↔ kolonne, navigasjon ↔ fremmednøkkel.
- **Spørringer:** LINQ oversettes til SQL. Alt før `ToListAsync()` er bare en *beskrivelse* av
  en spørring (`IQueryable`). SQL-en sendes først når du ber om resultatet. Det som ikke kan
  oversettes, gir en exception, ikke stille kjøring i minnet. Unntaket er den siste `Select`-en:
  der kjører EF det den ikke kan oversette, i C# på klienten etter at radene er hentet.
- **Endringssporing:** den husker hva du leste, og finner selv ut hva som må lagres.
- **Migreringer:** endringer i modellen blir versjonerte skjemaendringer
  (`dotnet ef migrations add …`).

**`DbContext` er to klassiske mønstre i én klasse:**
- **Unit of Work:** alle endringer samles opp og lagres samlet, i én transaksjon, når du kaller
  `SaveChanges()`.
- **Identity Map:** leser du samme rad to ganger i samme context, får du *samme objekt*
  tilbake. Én rad, ett objekt, per context.

**Change trackeren** gir hver entitet contexten kjenner til, en tilstand:

| Tilstand | Betyr | Ved `SaveChanges()` |
| --- | --- | --- |
| `Added` | Ny, finnes ikke i databasen | `INSERT` |
| `Unchanged` | Lest, ikke endret | ingenting |
| `Modified` | Lest, og minst én egenskap er endret | `UPDATE` |
| `Deleted` | Markert for sletting | `DELETE` |
| `Detached` | Contexten kjenner den ikke | ingenting, den er usynlig |

**Lastestrategier:** når du henter en `Order`, hva skjer med `Lines`?
- **Eager:** du ber om det i spørringen (`Include`). Én spørring, alt kommer med.
- **Explicit:** du laster det senere, eksplisitt (`db.Entry(order).Collection(o => o.Lines).LoadAsync()`).
- **Lazy:** det lastes av seg selv i det du rører navigasjonen. Behagelig, og farlig.
- **EF Core sin standard er ingen av delene.** Uten `Include` blir navigasjonene bare stående
  tomme. Med ett unntak: har contexten *allerede* lest de relaterte entitetene, kobler EF dem
  sammen av seg selv (*relationship fix-up*). Så noen ganger virker det, avhengig av hva som
  tilfeldigvis ble lest før i samme context. Det er en ekte felle.
- **Hibernate (JPA) er annerledes:** samlinger (`@OneToMany`) er lazy som standard, mens
  enkeltreferanser (`@ManyToOne`, som linje → bønne → brenneri) er *eager*, med mindre du setter
  dem til `LAZY`, slik Java-sporet gjør. Den forskjellen er hele nivå 1.

---

## Kaffebaren: domenet og dataene

Her er alt du trenger å vite om modellen og dataene før du starter. Koden ligger i
`Kaffebar.Orm/Domain/`, oppsettet i `Data/KaffebarDbContext.cs` og dataene i `Data/SeedData.cs`.

### Entitetene og tabellene

```
Order ──< OrderLine >── Bean >── Roastery
  │
  └──< StatusChange
```

Les `──<` som «én til mange»: én ordre har mange linjer, og hver linje har én bønne.

| Entitet | Tabell | Egenskaper | Relasjoner |
| --- | --- | --- | --- |
| `Order` | `orders` | `Id` (`Guid`), `CustomerName`, `Status`, `CreatedAt`, `TotalPrice` | Har mange `Lines` og mange `StatusHistory` |
| `OrderLine` | `order_lines` | `Id` (tall, lages av databasen), `DrinkName`, `Size`, `MilkType` (kan være tom), `ExtraShot`, `Quantity`, `UnitPrice` | Hører til én `Order` (`OrderId`) og har én `Bean` (`BeanId`) |
| `Bean` | `beans` | `Id`, `Name`, `Origin`, `Process` | Brent av én `Roastery` (`RoasteryId`) |
| `Roastery` | `roasteries` | `Id`, `Name` (unik), `City` | Har mange `Beans` |
| `StatusChange` | `status_changes` | `Id`, `Status`, `ChangedAt` | Hører til én `Order` (`OrderId`). Den har ingen navigasjon tilbake til ordren |

Verdt å vite når du leser SQL-en:

- **Tabeller og kolonner er i snake_case.** `Order.CreatedAt` blir `orders.created_at`, og `OrderLine.BeanId`
  blir `order_lines.bean_id`.
- **Fremmednøkkelen står på mange-siden.** `orders` har ingen kolonne for linjene. Det er `order_lines.order_id`
  som peker på ordren. `Lines` og `StatusHistory` finnes bare i koden.
- **Fremmednøklene er påkrevd.** En linje kan ikke finnes uten en ordre og en bønne.
- **Enumene lagres som tekst** med navnet fra C#, så i rå SQL skriver du `'Ready'`, ikke `2`.

| Enum | Verdier |
| --- | --- |
| `OrderStatus` | `Pending` → `Brewing` → `Ready` → `PickedUp` |
| `CoffeeSize` | `Small`, `Medium`, `Large` |
| `MilkType` | `Whole`, `Skimmed`, `Oat`, `Soy` |

### Prisene

- **`UnitPrice`** er prisen per kopp da bestillingen ble lagt inn, ikke dagens menypris. Den er menyprisen,
  minus 5 kr for `Small`, pluss 10 kr for `Large` og pluss 8 kr for en ekstra shot.
- **`TotalPrice`** står på ordren, slik den sto på kvitteringen. Den er summen av `UnitPrice × Quantity` for
  alle linjene. Databasen regner den ikke ut. Det er koden som må holde den riktig.

Menyen er den samme som i samling 3, uten Chai Latte (den har ingen kaffebønner): Filterkaffe 39, Espresso 35,
Dobbel espresso 45, Americano 42, Cortado 47, Cappuccino 49, Kaffe Latte 52, Flat White 55 og Mocha 59 kr.

### Dataene

Dataene er de samme på hver maskin og ved hver kjøring, så tallene i loggen din er de samme som naboens. Hver
testklasse får sin egen ferske database med disse dataene.

| | Antall | Detaljer |
| --- | ---: | --- |
| Brennerier | 6 | Tim Wendelboe, Kaffa, Fuglen, Supreme Roastworks, Solberg & Hansen (alle i Oslo) og Jacobsen & Svart (Trondheim) |
| Bønner | 15 | 2–3 per brenneri |
| Ordrer | 100 | 4 håndlagde (under) og 96 generert |
| Ordrelinjer | 250 | 1–4 linjer per ordre, 1–3 kopper per linje |
| Statusendringer | 393 | Én rad for hvert steg ordren har vært gjennom |

De 96 genererte ordrene er spredt over seks hele måneder, fra mars til august 2026. De er alle `PickedUp`, og har
derfor fire rader i `StatusHistory`: `Pending`, `Brewing`, `Ready` og `PickedUp`.

Brenneriene finnes, men bønnene og salgstallene er oppdiktet.

### Fire kunder du blir kjent med

De fire første ordrene er håndlaget, med de samme kundene som i samling 3. De ble lagt inn om morgenen 31. august
2026, og testene bruker dem fordi de trenger noe helt bestemt. I testene heter id-ene `SeedData.AdaOrderId`,
`SeedData.KariOrderId` og så videre.

| Kunde | Status | Bestilte | Totalpris |
| --- | --- | --- | ---: |
| **Ada** | `Ready` | 1 × Kaffe Latte (Medium, havremelk), Huila fra Tim Wendelboe | 52 kr |
| **Kari** | `Brewing` | 2 × Cappuccino (Large, helmelk, ekstra shot), Kirinyaga fra Kaffa | 134 kr |
| **Jonas** | `Ready` | 1 × Espresso (Small), Huehuetenango fra Supreme Roastworks | 30 kr |
| **Ingrid** | `Pending` | 3 × Mocha (Medium, soyamelk), Yirgacheffe fra Fuglen, og 1 × Filterkaffe (Large), Nyeri fra Tim Wendelboe | 226 kr |

---

## Forarbeid (ca. 15 min)

> 🧰 **Før samlingen:** installer det du trenger og kjør testene én gang hjemme. Stegene står i
> [README.md, «Før samlingen»](./README.md#før-samlingen). Da er alt lastet ned, og du kan starte med en gang.

### Du trenger
- **.NET 10 SDK** (`dotnet --version` → `10.x`)
- **En container-runtime:** Podman, Docker Desktop, Rancher Desktop (i *dockerd/moby*-modus) eller
  Colima. Alle fungerer, og testene finner den selv. Detaljer i [README.md](./README.md).
- **Ingen containere i det hele tatt?** Pek miljøvariabelen `KAFFEBAR_POSTGRES` på en Postgres du
  har tilgang til. Se README.

### Kjør opp prosjektet
```bash
cd nivaa-1              # eller mappen for nivået du starter på
dotnet tool restore     # henter dotnet-ef (brukes i oppgave 4A)
dotnet test             # i nivaa-1: 2 røde
```

Den første kjøringen laster ned `postgres:18-alpine` og tar litt tid. Hver testklasse får sin egen
ferske database, migrert og seedet.

De røde testene viser hva som gjenstår på nivået. Slik skal det være, og det er dem du skal gjøre
grønne. Noen feiler med `NotImplementedException`, andre fordi en kollega har skrevet kode med en feil
i. README-en i hver mappe sier hvor mange grønne og røde tester du skal starte med.

### Se hva som skjer
Testene skriver SQL-en EF sender mens de kjører, og til slutt dataene du trenger for å forstå
oppgaven, i faste seksjoner:

- **`── Resultat`** (eller **`── I databasen etterpå`**): det testen sjekker, for eksempel antall
  linjer på kvitteringene eller prisen som faktisk ble lagret.
- **`── Kommandoer til databasen`**: hver setning EF sendte, gruppert på type og tabell. For en
  `SELECT` står tabellene den leser, og hvor mange kolonner og rader som kom tilbake. For en `UPDATE`
  står kolonnene den skriver. Under tabellen står antall rundturer til databasen og rader lest.
- **`── Change trackeren`**: entitetene contexten følger med på, gruppert på type og tilstand
  (`Unchanged`, `Modified`, `Added` …).

Hver oppgave sier hva du skal se etter. For å se utskriften, filtrerer du på et nivå eller en oppgave
(`Level1`, `Level1A` …) og skrur opp detaljnivået:

```bash
dotnet test --filter Level1 --logger "console;verbosity=detailed"
```

Eller bruk test-runneren i Rider, Visual Studio eller VS Code (C# Dev Kit). Den viser output per
test.

Blir det for mye SQL å lese, setter du `ShowSql` øverst i `Kaffebar.Orm.Tests/PostgresFixture.cs` til
`false`. Da forsvinner SQL-en, mens advarslene fra EF og seksjonene over står igjen. Sett den tilbake
til `true` når oppgaven ber deg se på SQL-en.

### Bli kjent
1. `Kaffebar.Orm/Data/KaffebarDbContext.cs`: modellen. Hvilke tabeller blir det? Hvorfor lagres
   enumene som tekst?
2. `Kaffebar.Orm/Data/KaffebarDb.cs`: det ene stedet en context blir konfigurert. `LogTo(...)` er
   EF sin SQL-logging, som loggeren `org.hibernate.SQL` i Hibernate.
3. `Kaffebar.Orm/Data/QueryCounter.cs`: en `DbCommandInterceptor` som teller spørringer, kommandoer
   og rader, og husker SQL-en til hver kommando. Den er kveldens måleinstrument, og du trenger ikke
   å endre den.
4. Prøv `ToQueryString()`: legg `using Microsoft.EntityFrameworkCore;` øverst i `Level1Tests.cs` og
   `output.WriteLine(db.Orders.Where(o => o.CustomerName == "Ada").ToQueryString());` inn i en test,
   og se SQL-en *uten* å kjøre den.

---

## Nivå 1: Den naive kvitteringen (to feller for prisen av én)
*Mål: Forstå hvordan EF Core laster relaterte data, og se N+1-problemet i praksis.*

Du jobber i `nivaa-1`.

> 📚 **Teori: N+1.** Du henter en liste med N ting i én spørring. Så går du gjennom lista, og for
> hver ting trigger koden din én spørring til for å hente noe den peker på. 1 + N spørringer i
> stedet for 1. Koden ser uskyldig ut, fordi spørringene er skjult bak en helt vanlig
> egenskap. I EF Core krever det lazy loading, og det er **av** som standard.

**Bakgrunn:**
Sjefen for kaffebaren vil ha en utskrift av alle bestillingene. For hver bestilling vil hun se
kundenavnet, og for hver kopp vil hun vite hvilken bønne som ble brukt og hvilket brenneri som
brente den.

### 1A: Den naive spørringen
1. Åpne `Kaffebar.Orm/KaffebarRepository.Level1A.cs` og skriv `GetAllOrdersAsync()`: alle ordrene,
   sortert på `CreatedAt`. Ikke noe mer.
2. Åpne `Kaffebar.Orm/Receipts.cs` og skriv `Write(...)`: for hver ordre, skriv kunde, status og
   totalpris. For hver linje, naviger ned til brenneriet (`order.Lines` → `line.Bean` →
   `bean.Roastery.Name`) og skriv det ut med `write(...)`.
3. Kjør `Level1A_NaiveQuery_ReturnsOrdersWithoutLines` og se på outputen: kvitteringene, og
   `Linjer på kvitteringene` mot `Linjer i databasen` under `── Resultat`.

Testen blir grønn. Men se på kvitteringene: **det er ingen kaffe på dem.** Ingen exception,
ingen advarsel. 100 kvitteringer med kundenavn og pris, og ingen linjer.

> 🤔 **Refleksjon i par:**
> * Hvorfor er `order.Lines` tom? Hvor mange spørringer ble kjørt?
> * Er dette bedre eller verre enn Hibernate, som ville hentet linjene i det stille?
> * Hvor i en ekte applikasjon ville denne feilen blitt oppdaget? Og av hvem?

### 1B: Skru på magien
1. Åpne `Kaffebar.Orm/Data/KaffebarDb.cs` og finn `TODO 1B`. Skru på lazy loading med
   `UseLazyLoadingProxies()`. Pakken er allerede installert.
2. Kjør `Level1B_LazyLoading_FillsTheReceipts_OneQueryAtATime` (den bruker `lazyLoading: true`).
3. Kvitteringene har kaffe nå. Se på SQL-en som dukker opp *mellom* kvitteringene, og på tabellen
   under `── Kommandoer til databasen`: hvilke tabeller ble lest, og hvor mange ganger?

> 💡 Se på `Domain/Order.cs`: alle navigasjonene er `virtual`. Lazy loading i EF Core lager en
> *proxy*, en subklasse av entiteten din generert under kjøring, som overstyrer navigasjonene og
> laster data i det du rører dem. Uten `virtual` får den ikke overstyrt noe, og EF nekter å starte.

> 🤔 **Refleksjon i par:**
> * Hvor mange SQL-spørringer ble kjørt for 100 ordrer? Hvilken tabell står for de fleste?
> * Den naive gjetningen er 1 + 100 + 250 + 250: én for ordrene, én per ordre for linjene (en
>   samling lastes i én spørring per *ordre*, ikke per linje), og så én per linje for bønnen og én
>   for brenneriet. Hvorfor ble det så mye færre? (Hint: Identity Map.)
> * Hva skjer med ytelsen på fredag ettermiddag, når det ligger 10 000 ordrer i databasen og
>   databasen står i et annet datasenter enn appen?
> * EF Core valgte å ha lazy loading **av** som standard. Hibernate laster samlinger lazy. Hvem
>   har rett?
> * Skriv ut `orders[0].GetType()` med lazy loading på. Hva får du, og hvor kommer den klassen fra?
> * Loggen viser parameterverdiene fordi `KaffebarDb` skrur på `EnableSensitiveDataLogging()`. Hva
>   ser du uten den? Hvorfor skal den aldri være på i produksjon?

---

## Nivå 2: Ta tilbake kontrollen
*Mål: Løse N+1, velge riktig lasteverktøy, skrive gjennom relasjonene, og skrive tester som sikrer ytelsen fremover.*

> 📚 **Teori: Tre måter å si hva du trenger på.**
> * **`Include` / `ThenInclude`**: last entitetene *og* grafen under dem i samme spørring (EF sin
>   `JOIN FETCH`). Du får sporede entiteter du kan endre.
> * **Projeksjon (`Select`)**: be om akkurat de kolonnene du skal bruke, rett inn i en DTO. EF
>   skjønner selv hvilke tabeller den må joine. Ingen entiteter, ingen change tracking. Ofte det
>   beste valget for noe som bare skal *vises*.
> * **Split query (`AsSplitQuery()`)**: én spørring per samling i stedet for én stor JOIN.
>   Hvorfor det trengs, ser du i 2B.

**Bakgrunn:**
Vi kan ikke ha et system som fyrer av over hundre spørringer for å skrive ut 100 kvitteringer. Vi
må fortelle databasen at den skal gjøre den tunge løftingen.

Du jobber i `nivaa-2`. Der er nivå 1 allerede løst.

### 2A: Include
1. Åpne `Kaffebar.Orm/KaffebarRepository.Level2A.cs` og skriv `GetAllOrdersWithDetailsAsync()`,
   som henter ordrer, linjer, bønner og brennerier i én og samme spørring.
2. Kjør `Level2A_Include_LoadsTheWholeReceiptInOneQuery`. Testen sjekker at hver linje har bønne
   og brenneri, og bruker kveldens måleinstrument: `Assert.Equal(1, counter.SelectCount)`.
3. Se på SQL-en i loggen. Hvilke JOIN-er ble det?

Så samme kvittering på den andre måten, med en projeksjon:

4. Skriv `GetReceiptsAsync()` i samme fil. Den bygger `ReceiptDto` (se `Dtos.cs`) direkte i spørringen
   med `Select`, uten `Include`.
5. Kjør `Level2A_Projection_LoadsTheWholeReceiptInOneQuery` og sammenlign SQL-en med den fra
   `Include`. Hvilke kolonner hentes nå? Sammenlign også `Kolonner` under
   `── Kommandoer til databasen` og `── Change trackeren` i de to testene.
6. Testen sjekker også at change trackeren er tom. Hvorfor er den det?
7. Hibernate kan skille *hva* du henter fra *hvordan* med en `EntityGraph`. EF har ikke det, men du kan
   flytte `Include`-kjeden til en utvidelsesmetode, `WithReceiptDetails(this IQueryable<Order> q)`. Hva
   er fordelen?

### 2B: To samlinger
1. Åpne `Kaffebar.Orm/KaffebarRepository.Level2B.cs` og skriv `GetOrdersWithLinesAndHistoryAsync()`,
   som tar med *både* `Lines` (helt ned til brenneri) *og* `StatusHistory`. Skriv den rett fram
   med to `Include` først.
2. Kjør `Level2B_TwoCollections_DoNotMultiplyTheRows`. Sammenlign `Rader med data totalt` under
   `── Dataene` med `Rader` under `── Kommandoer til databasen`, og se på advarselen i loggen
   (`MultipleCollectionIncludeWarning`).
3. Fiks det, og se at antall spørringer og rader endrer seg.

### 2C: Kassa-appen skriver

> 📚 **Teori: Skriving gjennom relasjoner.** EF har ingen «eierside» du må huske på. Du kan sette
> fremmednøkkelen (`BeanId = 3`), sette navigasjonen (`line.Bean = bean`) eller legge til i samlingen
> (`order.Lines.Add(line)`). EF holder alle tre i takt (*relationship fix-up*). Fjerner du en entitet
> fra en lastet samling, kommer det an på relasjonen: er fremmednøkkelen påkrevd, blir raden slettet.
> Er den valgfri, blir fremmednøkkelen satt til `NULL`, og raden blir stående.

**Bakgrunn:**
Til nå har vi bare lest. Kassa-appen må også registrere bestillinger og fjerne linjer når kunden
angrer. En kollega har skrevet `Kaffebar.Orm/KaffebarRepository.Level2C.cs`.

1. Kjør `dotnet test --filter Level2C --logger "console;verbosity=detailed"`. Begge testene er røde.
2. `RegisterOrderAsync` lagrer linjene, men totalprisen blir 0: se `TotalPrice` mot `Sum av linjene`
   under `── I databasen etterpå`. Lag en domenemetode `Order.AddLine(...)` i `Domain/Order.cs` som
   holder totalprisen riktig, og bruk den.
3. `RemoveLineAsync` kjører uten feil, men linjen ligger fortsatt i databasen. Se hvilke kommandoer
   den sendte. Hvorfor? (Hint: hva inneholder `order.Lines` her? Du har sett det før.)
4. Få testen grønn: last det du trenger, og lag en domenemetode `Order.RemoveLine(...)` som holder både
   samlingen og totalprisen i orden.
5. Eksperiment: bruk `db.OrderLines.Remove(line)` i stedet for domenemetoden. Blir linjen slettet? Hva
   skjer med totalprisen?

> 🤔 **Refleksjon i par:**
> * EF har en EAGER-ekvivalent: `modelBuilder.Entity<Order>().Navigation(o => o.Lines).AutoInclude()`.
>   Hva er faren ved å bruke det som standard på entitetene i stedet for `Include` i spørringen?
> * Med to samlinger i én JOIN får hver ordre *linjer × statusendringer* rader. Hvor mange rader er
>   det for en ordre med 4 linjer og 4 statusendringer? Og hvis vi hadde lagt til en tredje samling?
> * Hibernate kaster `MultipleBagFetchException` her. EF gir deg bare en advarsel i loggen. Hva
>   foretrekker du?
> * Split query gir flere rundturer mot databasen, og spørringene går ikke i samme snapshot. Når
>   er én stor JOIN likevel det riktige valget?
> * Når ville du valgt `Include` framfor projeksjon?
> * I Java-sporet feiler `getLinjer().add(...)` fordi Hibernate ser på eiersiden av relasjonen. Hvorfor
>   virker `order.Lines.Add(...)` i EF?
> * `OrderLine.OrderId` er en påkrevd `Guid`. Hva ville skjedd med raden i `RemoveLine` hvis den var
>   `Guid?`? Hvor sikker er du på at det er det du vil?
> * `Lines.Remove(line)` bruker referanselikhet, for entitetene våre overstyrer ikke `Equals`. Hvorfor
>   går det bra her? Hva ville skjedd med en `HashSet<OrderLine>` hvis `GetHashCode` var basert på
>   `Id`, og linjen ble lagt i settet *før* den ble lagret?
> * Hvorfor bør `Lines` egentlig ikke kunne endres utenfra?
> * Hvordan sikrer en test med `QueryCounter` at ingen innfører N+1 igjen om et halvt år?

---

## Nivå 3: Kampanjetabben (change tracking)
*Mål: Forstå entitetenes livssyklus og change trackeren, og farene ved ufrivillige dataendringer.*

Du jobber i `nivaa-3`. Der er nivå 1 og 2 allerede løst.

> 📚 **Teori: Tracking og `SaveChanges`.** Alt du leser med en vanlig spørring, blir *sporet*:
> contexten tar et øyeblikksbilde av verdiene. Når `SaveChanges()` kalles, sammenlignes hver
> sporede entitet med bildet sitt, og **alt** som er endret, skrives til databasen. Ikke bare
> det du tenkte på da du kalte `SaveChanges`, men alt contexten har sett siden den ble laget.
>
> Sammenligningen heter `DetectChanges()`, og `SaveChanges` kjører den automatisk.
> `ChangeTracker.DebugView` gjør det ikke, så testen kaller `DetectChanges()` før den skriver ut
> hva contexten sporer.
>
> Dette er en viktig forskjell fra Hibernate. Hibernate *flusher automatisk* ved commit, så en
> endret entitet lagres selv om du aldri ba om det. EF Core lagrer **bare** når noen kaller
> `SaveChanges()`, og en transaksjon som committes uten det, lagrer ingenting. Høres tryggere ut.
> Er det det?
>
> Verktøy: `AsNoTracking()` leser uten å spore. En projeksjon (`Select` til DTO) gir ingen
> entiteter å spore i det hele tatt. `db.ChangeTracker.DebugView.LongView` viser alt contexten
> følger med på akkurat nå.

**Bakgrunn:**
Kaffebaren kjører en kampanje! Alle kunder som har kjøpt bønner fra **Tim Wendelboe**, skal få
**15 % rabatt**. Rabatten gjelder *kun for visning* i en ny kampanje-app. Ordren skal **ikke**
endres i databasen. Kvitteringen er et historisk dokument.

### 3A: Tabben
1. Åpne `Kaffebar.Orm/KaffebarRepository.Level3A.cs` og skriv `GenerateCampaignReceiptAsync(Guid orderId)`
   rett fram, slik `TODO 3A` beskriver:
   - Hent ordren med linjer, bønner og brenneri.
   - Inneholder den bønner fra `Campaign.RoasteryName`? Sett `order.TotalPrice` til kampanjeprisen
     (15 % rabatt, avrundet til to desimaler).
   - Returner en `CampaignReceipt` med ordinær pris og kampanjepris.
2. Du kaller aldri `SaveChanges()`. Trygt, ikke sant? Kjør testene for 3A:
   ```bash
   dotnet test --filter Level3A --logger "console;verbosity=detailed"
   ```

**Vent nå litt… Hvorfor har Ingrid fått rabatten lagret i databasen?!**

Se på `Level3A_CampaignReceipt_NeverChangesTheOrderInTheDatabase`. Testen gjør det en helt vanlig
request i et API ville gjort: én context, flere operasjoner. Først vises kampanjekvitteringen.
Så flytter baristaen Karis ordre videre med `UpdateStatusAsync` (i `KaffebarRepository.cs`), en
metode som var ferdig skrevet og som ikke har noe med kampanjen å gjøre. *Den* kaller
`SaveChanges()`.

3. Finn `── Change trackeren før baristaen lagrer` i test-outputen. Du skal se noe som
   `Order {Id: 0de50001-…-000000000004} Modified` og `TotalPrice: 192.10 Modified Originally 226.00`.
   Se så på `── Kommandoer fra UpdateStatusAsync (Kari)`. Hvem ba om at den skulle lagres?
   Change trackeren står også i de to testene som er grønne. Hvilken tilstand har ordren der?
4. **Refaktorer** slik at kampanjekvitteringen aldri kan mutere databasen, uansett hva andre kaller
   senere. Alle tre testene for 3A skal være grønne.
   * Les uten tracking, eller aller helst: projiser bare det du trenger (id, navn, pris og om
     ordren har Tim Wendelboe-bønner) rett inn i en anonym type eller DTO.
   * Flytt selve prisregelen ut i en ren funksjon i `Campaign.cs` (f.eks. `Campaign.Apply(decimal
     ordinaryPrice, bool eligible)`) som verken ser entiteter eller en `DbContext`.

> 🤔 **Refleksjon i par:**
> * Hvorfor lagret EF endringen din uten at *du* ba om det? Hvilken tilstand hadde Ingrids ordre?
> * I ASP.NET Core lever en `DbContext` typisk like lenge som én HTTP-request (*scoped*). Hvor
>   mange tjenester kan ha rørt den samme contexten før noen kaller `SaveChanges()`?
> * Hvordan bryter det å regne ut visningspriser på en sporet entitet med «Clean Architecture»? Hva
>   hører hjemme i et domenelag, og hva i et datalag?
> * Er `AsNoTracking()` alene nok? Hva skjer om noen senere gjør `db.Update(order)` på et
>   ikke-sporet objekt de har endret?
> * Den naive metoden returnerte en DTO, men skadet databasen likevel. Hva er det egentlig DTO-en
>   beskytter mot, og hva beskytter den *ikke* mot?

### 3B: Entiteter ut av datalaget

**Bakgrunn:**
Kassa-appen vil ha en bestilling som JSON. I Java-sporet er dette et REST-endepunkt. Her har vi ikke
noe API, men feilene er de samme, for de oppstår når entiteten blir til JSON. En kollega har skrevet
`GetOrderJsonAsync` i `Kaffebar.Orm/KaffebarRepository.Level3B.cs`, og formatet kassa-appen forventer,
står i kommentaren over metoden.

1. Kjør `dotnet test --filter Level3B --logger "console;verbosity=detailed"`. Sammenlign
   `── Kassa-appen forventer` med `── GetOrderJsonAsync ga`. Hvor er linjene, og hva betyr
   `"status": 0`? Det kom ingen feilmelding.
2. Prøv den «åpenbare» løsningen: `Include` alt, og serialiser fortsatt entiteten. Hva skjer nå? (Hint:
   `OrderLine.Order`.)
3. Gjør det riktig: lag `record`-er med formatet fra kommentaren, og projiser rett inn i dem. Tenk på
   det du lærte på nivå 2: hvor mange spørringer bruker metoden? Testen tillater maks tre, og
   krever at change trackeren er tom etterpå.

> 🤔 **Refleksjon i par:**
> * Hvorfor er det en dårlig idé å eksponere entiteter direkte, selv når det «fungerer»? (Hint:
>   `createdAt` står i JSON-en fra steg 1. Hvem har bestemt det?)
> * Hva skjer hvis du serialiserer en entitet med lazy loading på, mens contexten lever? Og etter at
>   den er disposet? Prøv begge.
> * `ReferenceHandler.IgnoreCycles` får bort unntaket fra steg 2. Er det en løsning?

### 3C (bonus): Masseoppdatering

> 📚 **Teori: Bulk-operasjoner.** `ExecuteUpdateAsync` og `ExecuteDeleteAsync` oversetter en LINQ-
> spørring rett til én `UPDATE` eller `DELETE`, uten å laste noe inn i minnet. Men de går **forbi
> change trackeren**. Ingen entiteter blir oppdatert i minnet, og ingen domenelogikk kjører. Til sammenligning *batcher* `SaveChanges` endringene sine, så mange
> rader går i få rundturer til databasen. Forskjellen er ikke alltid der du tror.

**Bakgrunn:**
Baristakøen er full av ordrer som har stått `Ready` lenge. Alt som ble bestilt før en gitt dato,
har kunden hentet.

1. Åpne `Kaffebar.Orm/KaffebarRepository.Level3C.cs` og skriv `PickUpStaleOrdersAsync(DateTimeOffset cutoff)`
   først med en løkke: hent de sporede `Ready`-ordrene, sett `PickedUp`, legg til en `StatusChange`
   på hver, og `SaveChangesAsync()`.
2. Kjør `Level3C_StaleReadyOrders_ArePickedUp_AndTheHistoryIsKept`. Noter tre tall fra outputen:
   `Rundturer til databasen`, `Rader lest` og `Sporet totalt`, og se hvilke setninger som ble sendt.
3. Skriv den om med `ExecuteUpdateAsync`, og sammenlign de tre tallene og setningene.
4. Testen krever at historikken er med: hver ordre skal få en `PickedUp`-rad i `StatusHistory`.
   Hvordan får du til det uten å laste entitetene? (Hint:
   `db.Database.ExecuteSqlAsync($"INSERT … SELECT …")` finnes.) Og hvordan sørger du for at
   statusen og historikken lagres sammen, eller ikke i det hele tatt? Tabellen heter `status_changes`,
   med kolonnene `order_id`, `status` og `changed_at`. Status lagres som tekst med C#-navnet:
   `'Ready'`, `'PickedUp'`.

> 🤔 **Refleksjon i par:**
> * Ble antall kommandoer det samme i de to variantene? Hvorfor, eller hvorfor ikke? (Hint: se på
>   hvordan `SaveChanges` sender flere `UPDATE`/`INSERT` til databasen, og på hvordan du skrev
>   historikken.)
> * Hva er det da som faktisk skiller dem? Se på radene i minnet og entitetene i change trackeren.
>   Hvordan ser de tallene ut med 10 000 gamle ordrer?
> * Hva hopper `ExecuteUpdate` over? Tenk på historikken, og på ordrer contexten allerede har lastet
>   (hvilken status har de i minnet etterpå?). Kommer du tilbake hit etter 4A: hva med versjonen?
> * I Java-sporet setter en bulk-`UPDATE` statusen, og så skriver Hibernate den gamle statusen tilbake
>   når prisen på en lastet entitet lagres, fordi den skriver alle kolonnene. Prøv det samme i EF: hent
>   Ada, sett statusen med `ExecuteUpdateAsync`, endre `TotalPrice` på entiteten og kall
>   `SaveChangesAsync`. Hva skjer med statusen, og hvorfor?

### 3D (bonus): Den motsatte fellen

**Bakgrunn:**
Kassa-appen henter en bestilling, endrer status og sender den inn igjen til `SaveOrderAsync` i
`Kaffebar.Orm/KaffebarRepository.Level3D.cs`. Kollegaen har lært av 3A og tenker at «EF oppdager
endringer automatisk».

1. Kjør `dotnet test --filter Level3D --logger "console;verbosity=detailed"`. Ble endringen lagret? Fikk
   du noen feilmelding? Se `── Kommandoer fra SaveOrderAsync`.
2. Prøv `db.Update(order)` før `SaveChangesAsync`. Den første testen blir grønn, men den andre er rød.
   Se på `UPDATE`-setningen under `── Kommandoer fra SaveOrderAsync`. Hvilke kolonner skriver den? Hvor
   ble det av prisen noen andre satte?
3. Få begge testene grønne. Trenger du egentlig hele objektet? Se på hva som allerede finnes i
   `KaffebarRepository.cs`.

> 🤔 **Refleksjon i par:**
> * Hva er forskjellen på en *sporet* og en *detached* entitet? Hva er `SaveChanges` sin returverdi i
>   steg 1?
> * Hva gjør `db.Update(order)` med `order.Lines` hvis linjene var lastet? Hva er forskjellen på
>   `Update` og `Attach`, og hva gjør `db.Entry(order).Property(o => o.Status).IsModified = true`?

---

## Nivå 4: Kollisjoner og rå kraft
*Mål: Håndtere samtidighet med versjonering, og vite når man skal kaste ORM-rammeverket på båten.*

Du jobber i `nivaa-4`. Der er nivå 1, 2 og 3 allerede løst.

### 4A: Kollisjonen (optimistisk låsing)

> 📚 **Teori: Optimistisk vs. pessimistisk låsing.** *Pessimistisk*: lås raden når du leser den
> (`SELECT … FOR UPDATE`), så ingen andre kan endre den før du er ferdig. Trygt, men andre må vente,
> og låser kan henge. *Optimistisk*: ikke lås, men ta med en versjon. Ved `UPDATE` sier du «bare
> hvis versjonen fortsatt er den jeg leste» (`WHERE id = … AND version = …`). Ble 0 rader
> oppdatert, var noen andre først, og EF kaster `DbUpdateConcurrencyException`.
>
> Postgres har allerede en versjon på hver rad: systemkolonnen **`xmin`**, som endres ved hver
> oppdatering. Npgsql mapper en `[Timestamp] uint`-egenskap rett til den.

**Bakgrunn:**
To baristaer ser at Jonas sin espresso er klar. Begge roper navnet hans og markerer ordren som
hentet, på nøyaktig samme tidspunkt.

1. Kjør `Level4A_TwoBaristas_OnlyOneCanHandOutTheCoffee` først, uten å endre noe. Testen bruker to
   contexter (to baristaer) mot samme ordre. Hvorfor feiler den? Se `── Resultat`: hva svarte hver
   barista, hvor mange `PickedUp`-rader fikk Jonas i historikken, og hva skjedde med versjonen
   (`xmin`) mellom dem? Dette er *last write wins*.
2. Legg til et versjonsfelt i `Kaffebar.Orm/Domain/Order.cs`:
   ```csharp
   using System.ComponentModel.DataAnnotations;   // øverst i fila

   [Timestamp]
   public uint Version { get; set; }
   ```
3. Modellen er endret, så databasen må få vite det. Lag en migrering:
   ```bash
   dotnet ef migrations add AddOrderVersion --project Kaffebar.Orm
   ```
   Se på migreringen som ble generert. Så se på SQL-en den faktisk blir til:
   ```bash
   dotnet ef migrations script InitialCreate AddOrderVersion --project Kaffebar.Orm
   ```
   Migreringen sier `AddColumn`. Hvor ble det av `ALTER TABLE`?
4. Kjør testen igjen. Nå skal du få en `DbUpdateConcurrencyException`. Fang den i
   `MarkAsPickedUpAsync` i `Kaffebar.Orm/KaffebarRepository.Level4A.cs` og returner
   `PickupResult.Conflict`.
5. Til slutt sjekker testen at barista 2 sin context er *ryddet*:
   `Assert.False(db2.ChangeTracker.HasChanges(), …)`. Se `── Change trackeren til barista 2`, og
   feilmeldingen, som viser `DebugView.ShortView` av det som fortsatt venter på å bli lagret. Hva ligger igjen i change
   trackeren etter en feilet `SaveChanges`? (Hint: den nye `StatusChange`-en, og ordren slik
   barista 2 tror den er. `ReloadAsync()` finnes.)

**Del 2: Kollisjonen mellom to kall.** Kollisjoner skjer også *mellom* kall: baristaen ser bestillingen
på skjermen, og noen andre endrer den før baristaen rekker å trykke «Hentet». I et API sender klienten
da versjonen den så, for eksempel i headeren `If-Match`. Vi har ikke noe API, så her er versjonen bare
et parameter.

6. Skriv `MarkAsPickedUpAsync(Guid orderId, uint expectedVersion)` i samme fil. Prøv først det som ser
   riktig ut: hent ordren, sett `order.Version = expectedVersion` og kall metoden fra steg 4.
7. Kjør `dotnet test --filter Level4AClient --logger "console;verbosity=detailed"`. Klienten med gammel
   versjon får `PickedUp`, ikke `Conflict`. Se på `WHERE xmin = @p2` i loggen og verdien til `@p2`, og
   sammenlign med `Versjon klienten så` og `Versjon i databasen nå`. Hvilken versjon sjekket EF mot?
8. EF sammenligner med *originalverdien*, altså det den leste fra databasen. Sett den i stedet:
   `db.Entry(order).Property(o => o.Version).OriginalValue = expectedVersion`.

> 🤔 **Refleksjon i par:**
> * Når vi fanger en `DbUpdateConcurrencyException` i et REST-API, hvilken HTTP-statuskode bør vi
>   returnere til klienten? Hva er forskjellen på `409 Conflict` og `412 Precondition Failed` med
>   `ETag`/`If-Match`?
> * Bør backend automatisk prøve på nytt (retry), eller er det klientens ansvar? Er svaret det
>   samme for «marker som hentet» og for «legg til en kopp i bestillingen»?
> * Ble historikkraden til barista 2 lagret? Hvorfor ikke?
> * I Java-sporet kommer konflikten pakket inn i en `RollbackException` ved commit. Hva får du i EF, og
>   hva ville det bety for en felles feilhåndtering i et API?
> * Når ville du valgt pessimistisk låsing (`SELECT … FOR UPDATE`) i stedet?

### 4B: Den komplekse lesemodellen

> 📚 **Teori: Når ORM er feil verktøy.** En ORM er laget for å lese og skrive *entiteter*: ta en
> graf ut, endre den, lagre den. Rapporter er noe annet: aggregater over tusenvis av rader, der
> resultatet ikke er en entitet i det hele tatt. Da vil du at databasen gjør jobben, og da er SQL
> det presise språket. EF Core lar deg skrive rå SQL og mappe resultatet til en vilkårlig type med
> `db.Database.SqlQuery<T>(...)`. Interpolerte verdier blir parametre, ikke strengsammensetting.
>
> «Topp N per gruppe» er et klassisk tilfelle. `GROUP BY … ORDER BY … LIMIT 3` gir topp 3
> *totalt*, ikke per gruppe. Det du trenger er en **vindusfunksjon**:
> `ROW_NUMBER() OVER (PARTITION BY gruppe ORDER BY …)`, og så filtrere på den.

**Bakgrunn:**
Ledelsen krever et nytt dashboard: *De 3 mest solgte bønnene per brenneri, per måned, målt i antall
kopper, inkludert omsetning.* Å laste alle linjene som entiteter og regne i C# ville kreve mye minne
og masse unødvendig trafikk.

1. Åpne `Kaffebar.Orm/KaffebarRepository.Level4B.cs` og skriv `GetTopBeansPerRoasteryPerMonthAsync()`
   med `db.Database.SqlQuery<BeanSalesRow>($"...")`.
2. Kjør `Level4B_TopBeans_MatchesTheSameCalculationInCSharp`. Testen regner ut den samme rapporten
   på den tunge måten (alle linjene inn i minnet, LINQ to Objects) og sammenligner. Din versjon skal
   gjøre det i **én** spørring, og bare rapportradene skal over nettverket: testen sjekker at antall
   rader databasen sendte (`Rader lest`), er lik antall rader i rapporten (`Rader i rapporten`). Å hente alle linjene og regne i C#
   feiler derfor.

Detaljer testen er nøye på:
* Tabellene heter `orders`, `order_lines`, `beans` og `roasteries`. Kolonnene er i snake_case
  (`created_at`, `unit_price`, `roastery_id` …).
* Resultatkolonnene må hete det samme som egenskapene i `BeanSalesRow`, i snake_case: `month`,
  `roastery`, `bean`, `cups_sold`, `revenue`, `rank`.
* `month` er første dag i måneden som `date` (i UTC), for å bli en `DateOnly`.
* `cups_sold` og `rank` er `int`. `SUM` og `ROW_NUMBER` gir `bigint` i Postgres. Npgsql leser det som
  `int`, men `::int` gjør typen tydelig.
* Rangering innen hver måned og hvert brenneri: flest kopper, så høyest omsetning, så bønnenavn.
* Sortering av resultatet: brenneri, måned, rang.

**Bonus:**
* Prøv å skrive den samme rapporten i LINQ (`GroupBy` og `Take(3)` per gruppe). Klarer EF å
  oversette den? Hvordan ser SQL-en ut (`ToQueryString()`)? Hvilken versjon vil du vedlikeholde?
* Trenger du noe `SqlQuery` ikke kan, finnes den helt rå bakdøren: `db.Database.GetDbConnection()`
  gir deg ADO.NET-tilkoblingen, og derfra `DbCommand` eller Dapper. Det er EF sin motpart til Hibernate
  sin `doReturningWork(...)`. Skriv `GetTopBeansWithAdoNetAsync()` med den, og kjør
  `Level4B_Bonus_AdoNet_GivesTheSameReport`. Se på loggen og på tallet fra `QueryCounter`. Hvorfor er
  de tomme?

> 🤔 **Refleksjon i par:**
> * Hvor går grensen for når du slutter å bruke LINQ og skriver SQL selv?
> * Rå SQL bryter med refaktoreringsverktøyet ditt: gi `UnitPrice` et nytt navn, og kompilatoren
>   sier ingenting. Hvordan beskytter du deg? (Hint: se på testen.)
> * Legg inn en ny ordre med `db.Orders.Add(...)` uten å lagre, og kjør en spørring rett etterpå i samme
>   context. Ser den ordren? I Java-sporet flusher Hibernate før en native query, men ikke før JDBC.
>   Hva gjør EF, og hva må du gjøre for å se ordren i samme transaksjon?

### 4C (bonus): Black Friday-importen

> 📚 **Teori: Batching.** `SaveChanges` samler `INSERT`-er og `UPDATE`-er i batcher, og sorterer dem
> selv etter tabell. Du trenger ikke skru på noe. Men hver `SaveChanges` er sin egen transaksjon, og
> alt du legger til, blir liggende i change trackeren, med et øyeblikksbilde av hver entitet, til du
> sier noe annet. `db.ChangeTracker.Clear()` tømmer den.

**Bakgrunn:**
Nettbutikken sender 1000 bestillinger med 2 linjer hver i én fil. En kollega har skrevet
`ImportOrdersAsync` i `Kaffebar.Orm/KaffebarRepository.Level4C.cs`.

1. Kjør `dotnet test --filter Level4C --logger "console;verbosity=detailed"`. Hvor mange kommandoer går
   det til databasen, og hvor lang tid tar det? Hvor mange av de 250 bestillingene i den andre testen
   ble liggende etter feilen på siste linje?
2. Flytt `SaveChangesAsync` ut av løkka, og kjør igjen. Hvor mange kommandoer nå, og hvor mye raskere?
   Hvor mange entiteter ligger igjen i change trackeren?
3. Gjør importen minnevennlig: legg inn bolker på 100 (`newOrders.Chunk(100)`), med `SaveChangesAsync`
   og `db.ChangeTracker.Clear()` for hver bolk. Hvor mange bestillinger blir nå liggende etter feilen?
4. Få begge testene grønne: hele fila skal lagres, eller ingenting.

> 🤔 **Refleksjon i par:**
> * `OrderLine.Id` er en identity-kolonne som databasen genererer. Java-sporet spør hva som skjer med
>   batchingen hvis du bruker `IDENTITY`. Hva viste tallene dine i EF? Hvordan får EF tilbake id-ene?
> * Hva måtte du passe på med bønnene etter `Clear()`? Hvorfor er `BeanId` enklere her enn å hente
>   `Bean`-objektet?
> * Når ville du droppet EF helt for en import, og brukt for eksempel `COPY` i Postgres
>   (`BeginBinaryImport` i Npgsql)?

---

## Valgfrie oppgaver

For deg som blir ferdig før tiden, eller vil grave dypere. De hører ikke til noe nivå, og du trenger
dem ikke for å gjøre resten. Hver oppgave sier hvilket nivå den passer etter. Du gjør dem i mappen du
allerede jobber i.

Alt du lager her, er merket som valgfritt: filene, klassene, testene og migreringene heter
`Optional…`, og hver fil har en kommentar øverst. Da skiller de seg fra kursets egne filer, og
`dotnet test --filter Optional --logger "console;verbosity=detailed"` kjører bare dem.

### Valgfri 1: Kan ikke oversettes (passer etter nivå 1)

**Bakgrunn:** Sjefen lurer på hvor mange bestillinger som kommer i helgene.

1. Lag `Kaffebar.Orm/KaffebarRepository.Optional1.cs`, med en metode som ser helt rimelig ut:
   ```csharp
   using Kaffebar.Orm.Domain;
   using Microsoft.EntityFrameworkCore;

   namespace Kaffebar.Orm;

   // Valgfri oppgave 1: Kan ikke oversettes. Ikke en del av nivåene.
   public partial class KaffebarRepository
   {
       private static bool IsWeekend(DateTimeOffset time) =>
           time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

       /// <summary>Ordrene som ble lagt inn en lørdag eller søndag (UTC).</summary>
       public Task<List<Order>> GetWeekendOrdersAsync() =>
           db.Orders.Where(o => IsWeekend(o.CreatedAt)).ToListAsync();
   }
   ```
2. Lag `Kaffebar.Orm.Tests/Optional1Tests.cs`:
   ```csharp
   using Kaffebar.Orm.Data;
   using Microsoft.EntityFrameworkCore;
   using Xunit.Abstractions;

   namespace Kaffebar.Orm.Tests;

   /// <summary>Valgfri oppgave 1: Kan ikke oversettes. Ikke en del av nivåene.</summary>
   public class Optional1Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
   {
       [Fact]
       public async Task Optional1_WeekendOrders_AreFilteredInTheDatabase()
       {
           var counter = new QueryCounter();
           await using var db = fixture.CreateContext(output.WriteLine, counter);

           var weekend = await new KaffebarRepository(db).GetWeekendOrdersAsync();

           await using var check = fixture.CreateContext();
           var all = await check.Orders.ToListAsync();
           output.Heading("Resultat");
           output.Facts(
               ("Ordrer i databasen", all.Count),
               ("Helgeordrer", weekend.Count));
           output.Commands(counter);

           Assert.Equal(all.Count(o => o.CreatedAt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday), weekend.Count);
           // Databasen skal filtrere: bare helgeordrene kommer over nettverket.
           Assert.Equal(weekend.Count, counter.RowsRead);
       }
   }
   ```
3. Kjør `dotnet test --filter Optional1 --logger "console;verbosity=detailed"`. Les feilmeldingen. Hva
   prøvde EF å gjøre med `IsWeekend`?
4. Feilmeldingen foreslår selv en vei ut: hent med `ToListAsync()` først, og filtrer i C# etterpå. Prøv
   det. Testen er fortsatt rød. Sammenlign `Helgeordrer` med `Rader` under
   `── Kommandoer til databasen`.
5. Få testen grønn: skriv filteret slik at EF kan oversette det til SQL. Hva kan EF gjøre med
   `o.CreatedAt.DayOfWeek` direkte? Se på `WHERE` i SQL-en.

> 🤔 **Refleksjon i par:**
> * EF Core kaster en exception i stedet for å filtrere i C# for deg. Hvorfor er det et bedre valg?
> * Hvilken tidssone er «helg» i? Finn svaret i SQL-en.
> * Når er det helt greit å hente alt og regne i C#?

### Valgfri 2: Testen som stopper N+1 (passer etter nivå 2)

**Bakgrunn:** I 2A fikk du kvitteringene ned i én spørring. Om et halvt år fjerner noen en
`ThenInclude` i en refaktorering, og ingen merker det før kvitteringene går tregt i produksjon. Skriv
testen som stopper det. Java-sporet har den samme oppgaven.

1. Lag `Kaffebar.Orm.Tests/Optional2Tests.cs`:
   ```csharp
   using Kaffebar.Orm.Data;
   using Xunit.Abstractions;

   namespace Kaffebar.Orm.Tests;

   /// <summary>Valgfri oppgave 2: en test som stopper N+1 før den når produksjon. Ikke en del av nivåene.</summary>
   public class Optional2Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
   {
       [Fact]
       public async Task Optional2_Receipts_StayAtOneQuery()
       {
           // Skriv testen her.
       }
   }
   ```
2. Skriv testen: hent ordrene med `GetAllOrdersWithDetailsAsync()`, gå gjennom alt med
   `Receipts.Write(orders, _ => { })`, og sjekk antall spørringer med en `QueryCounter`. Se hvordan
   `Level2Tests` gjør det. `output.Commands(counter)` skriver ut tabellen.
3. Kjør den. Er den grønn?
4. Fjern `.ThenInclude(b => b.Roastery)` i `KaffebarRepository.Level2A.cs`, og kjør testen igjen. Ble den
   rød, og hva sier feilmeldingen? Prøv så med `lazyLoading: true` i `CreateContext`. Hvilken
   feilmelding forteller best hva som er galt?
5. Sett `ThenInclude` tilbake.

> 🤔 **Refleksjon i par:**
> * Hva bør feilmeldingen si, slik at den som får testen rød om et halvt år, skjønner hva som skjedde?
> * Hvilke andre metoder i et ekte prosjekt fortjener en slik test?

### Valgfri 3: Gammel data fra Identity Map (passer etter nivå 3)

**Bakgrunn:** En context lever gjerne gjennom en hel request, eller mye lenger. Hva ser den hvis noen
andre endrer en rad mens den lever?

1. Lag `Kaffebar.Orm.Tests/Optional3Tests.cs`:
   ```csharp
   using Kaffebar.Orm.Data;
   using Microsoft.EntityFrameworkCore;
   using Xunit.Abstractions;
   using static Kaffebar.Orm.Tests.Print;

   namespace Kaffebar.Orm.Tests;

   /// <summary>Valgfri oppgave 3: gammel data fra Identity Map. Ikke en del av nivåene.</summary>
   public class Optional3Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
   {
       [Fact]
       public async Task Optional3_SecondRead_SeesWhatOthersSaved()
       {
           var counter = new QueryCounter();
           await using var db = fixture.CreateContext(output.WriteLine, counter);
           var first = await db.Orders.SingleAsync(o => o.Id == SeedData.AdaOrderId);

           // Mens contexten lever, endrer noen andre prisen.
           await using (var other = fixture.CreateContext())
           {
               (await other.Orders.SingleAsync(o => o.Id == SeedData.AdaOrderId)).TotalPrice = 60m;
               await other.SaveChangesAsync();
           }

           var second = await db.Orders.SingleAsync(o => o.Id == SeedData.AdaOrderId);

           output.Heading("Resultat");
           output.Facts(
               ("Første lesing, TotalPrice", Kr(first.TotalPrice)),
               ("Andre lesing, TotalPrice", Kr(second.TotalPrice)),
               ("Samme objekt", ReferenceEquals(first, second)),
               ("I databasen, TotalPrice", Kr(60m)));
           output.Commands(counter);

           Assert.Equal(60m, second.TotalPrice);
       }
   }
   ```
2. Kjør `dotnet test --filter Optional3 --logger "console;verbosity=detailed"`. Se på `── Resultat` og
   `── Kommandoer til databasen`. Ble det sendt en ny spørring? Hvorfor har Ada fortsatt den gamle prisen?
3. Få testen grønn uten å endre det den andre contexten gjør. Finn minst to måter.
4. Bytt den andre lesingen til `await db.Orders.FindAsync(SeedData.AdaOrderId)`. Hvor mange spørringer
   ble det nå?

> 🤔 **Refleksjon i par:**
> * I 1B sparte Identity Map deg for mange spørringer. Her gir den deg gammel data. Er det den samme
>   mekanismen?
> * I ASP.NET Core lever en context i én request. Hvor stort er problemet der, og hvor stort er det i en
>   context som lever lenge, for eksempel i en bakgrunnsjobb?
> * Hvordan henger dette sammen med 4A?

### Valgfri 4: Migreringen som mister data (passer etter nivå 4)

**Bakgrunn:** I 4A leste du SQL-en en migrering blir til. Her ser du hvorfor det er en vane verdt å ha.
Du jobber i `nivaa-4`.

1. Legg til en kommentar fra kunden på `Order` i `Kaffebar.Orm/Domain/Order.cs`, og lag en migrering:
   ```csharp
   // Valgfri oppgave 4.
   public string? Note { get; set; }
   ```
   ```bash
   dotnet ef migrations add Optional4AddOrderNote --project Kaffebar.Orm
   ```
2. Produkteieren vil heller kalle den `Comment`. Gi egenskapen nytt navn, lag en migrering og les den.
   Hva gjettet EF?
   ```bash
   dotnet ef migrations add Optional4Experiment --project Kaffebar.Orm
   ```
   Fjern så eksperimentet igjen. `--force` gjør at EF ikke prøver å spørre en database om migreringen
   er tatt i bruk:
   ```bash
   dotnet ef migrations remove --force --project Kaffebar.Orm
   ```
3. Produkteieren vil også ha maks 200 tegn. Legg til `[MaxLength(200)]` på `Comment` (den trenger
   `using System.ComponentModel.DataAnnotations;`, som `[Timestamp]` i 4A), slik at navnebyttet og
   lengden kommer i samme migrering:
   ```bash
   dotnet ef migrations add Optional4RenameNoteToComment --project Kaffebar.Orm
   ```
   Les det `dotnet ef` skriver ut, og les migreringen. Hva skjer nå med dataene i `note`?
4. Lag `Kaffebar.Orm.Tests/Optional4Tests.cs`. Testen skriver en kommentar på Ada, går tilbake til
   migreringen før den siste, og fram igjen:
   ```csharp
   using Kaffebar.Orm.Data;
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Infrastructure;
   using Microsoft.EntityFrameworkCore.Migrations;
   using Xunit.Abstractions;

   namespace Kaffebar.Orm.Tests;

   /// <summary>Valgfri oppgave 4: en migrering som mister data. Ikke en del av nivåene.</summary>
   public class Optional4Tests(PostgresFixture fixture, ITestOutputHelper output) : IClassFixture<PostgresFixture>
   {
       private const string AdasComment = "Uten lokk, takk";

       [Fact]
       public async Task Optional4_TheLastMigration_KeepsTheData()
       {
           await using var db = fixture.CreateContext(output.WriteLine);
           var migrations = db.Database.GetMigrations().ToList();
           var migrator = db.GetService<IMigrator>();

           // Data i kolonnen, slik det ville vært i en database som har vært i bruk en stund.
           (await db.Orders.SingleAsync(o => o.Id == SeedData.AdaOrderId)).Comment = AdasComment;
           await db.SaveChangesAsync();

           // Tilbake til migreringen før den siste, og fram igjen.
           await migrator.MigrateAsync(migrations[^2]);
           await migrator.MigrateAsync();

           await using var fresh = fixture.CreateContext();
           var ada = await fresh.Orders.SingleAsync(o => o.Id == SeedData.AdaOrderId);
           output.Heading($"Ada, før og etter {migrations[^1]}");
           output.Facts(("Comment før", AdasComment), ("Comment etter", ada.Comment ?? "null"));

           Assert.Equal(AdasComment, ada.Comment);
       }
   }
   ```
5. Kjør `dotnet test --filter Optional4 --logger "console;verbosity=detailed"`. Se på
   `── Ada, før og etter …`, og på `ALTER TABLE` i loggen.
6. Rett migreringen for hånd, slik at dataene blir med begge veier, i både `Up` og `Down`. Kjør testen
   igjen, og se hva som står under `ALTER TABLE` nå.

> 🤔 **Refleksjon i par:**
> * EF gjettet riktig i steg 2, men ikke i steg 3. Hva var forskjellen?
> * Testene for nivåene var grønne hele tiden. Hvorfor merket de ikke at dataene forsvant?
> * Hvor i et ekte prosjekt bør noen lese migreringen før den kjøres mot produksjon?

---

## Oppsummering

Etter denne workshopen har dere sett:

- At EF Core sin standard er **ingen lazy loading**: tomme navigasjoner i stedet for skjulte
  spørringer. Og hvordan N+1 ser ut når du skrur det på likevel.
- **`Include`, projeksjon og split query**, og hvordan du måler antall spørringer og rader i en test.
- **Skriving gjennom relasjoner**, og at en samling som ikke er lastet, stille ignorerer det du gjør med den.
- At **`SaveChanges()` lagrer alt contexten sporer**, ikke bare det du tenkte på, hvorfor lesemodeller
  bør være projeksjoner, og hvorfor entiteter ikke hører hjemme i JSON.
- **Detached entiteter**, og hvorfor `db.Update` skriver mer enn du tror.
- **Optimistisk låsing** med Postgres sin `xmin`, også mellom to kall, og hva som må ryddes etter en konflikt.
- **Når ORM er feil verktøy**: vindusfunksjoner i rå SQL, bulk-operasjoner som går forbi change trackeren,
  og store importer.

Lykke til, og god kaffe! ☕
