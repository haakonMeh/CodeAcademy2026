# Kaffebaren: Et dypdykk i ORM og Hibernate
Velkommen til kveldens workshop! I dag skal vi utforske de mørkere krokene av objekt-relasjonell mapping (ORM). Vi har strippet vekk all Spring-magi. Her er det kun deg, vanilla Java, ren Hibernate, Jersey og en relasjonsdatabase.

Koden bygger på et domene for en kaffebar: `Bestilling` -> `Bestillingslinje` -> `Kaffebonne` -> `Brenneri`.

### Velg din sti
Oppgavene er delt inn i fire nivåer. For å fylle kvelden (ca. 3 timer koding), velger du det sporet som passer din erfaring best:
* **Fersk på Hibernate?** Ta Nivå 1 og 2.
* **Erfaren, men rusten på detaljene?** Ta Nivå 2 og 3.
* **ORM-veteran?** Hopp rett til Nivå 3 og 4.

Omtrentlig tid per nivå, inkludert refleksjon i par: nivå 1 ca. 45 min, nivå 2 ca. 90 min, nivå 3 og 4 ca. 60–75 min hver. Oppgaver merket *bonus* er valgfrie; ta dem hvis du har tid til overs.

Hvert nivå ligger i sin egen mappe (`nivaa-1`, `nivaa-2`, …) og er et selvstendig Maven-prosjekt. Mappen for et nivå inneholder løsningen på nivået før, så du kan starte hvor du vil.

### Kom i gang
Du trenger Java 25, Maven og Docker. Databasen (PostgreSQL) startes automatisk med Testcontainers og fylles med 5 brennerier, 15 kaffebønner og 100 bestillinger. Dataene er like for alle.

```bash
cd nivaa-1
mvn test                 # kjører testene
mvn compile exec:java    # starter appen på http://localhost:8080
```

Hvert nivå har tester som er **røde** når du starter. Du er ferdig med en oppgave når testen er grønn *og* du kan svare på refleksjonsspørsmålene.

`QueryCountLogger` teller SQL-setningene Hibernate sender til databasen. Den er din beste venn i kveld.

All logging styres fra `src/main/resources/logback.xml`. Der kan du slå av og på SQL-setningene (`org.hibernate.SQL`), parameterverdiene (`org.hibernate.orm.jdbc.bind`) og mer. Hvert valg er forklart i filen.

---

## Nivå 1: Den naive kvitteringen (N+1-fellen)
*Mål: Forstå objekt-relasjonell mismatch og se konsekvensene av lazy loading i praksis.*

**Bakgrunn:**
Sjefen for kaffebaren ønsker en utskrift av alle bestillingene. For hver bestilling vil hun se kundenavnet, og for hver kaffekopp vil hun vite navnet på brenneriet som leverte bønnene.

**Oppgaven:**
1. Slå på SQL-logging i `src/main/resources/logback.xml`: sett loggeren `org.hibernate.SQL` til `DEBUG`.
2. Åpne `KaffebarRepository` og implementer `hentAlleBestillinger()` med JPQL: `SELECT b FROM Bestilling b`.
3. Åpne `Kvitteringsskriver` og implementer `lagKvittering(...)`. Iterer over bestillingene og naviger ned til brenneriet (`bestilling.getLinjer() -> linje.getKaffebonne() -> kaffebonne.getBrenneri().getNavn()`).
4. Kjør `KvitteringTest` til den er grønn. Se på utskriften fra testen og på loggen.
5. Start appen og åpne http://localhost:8080/kvitteringer.

> 🤔 **Refleksjon i par:**
> * Hvor mange SQL-spørringer ble faktisk kjørt mot databasen? Hvilke typer spørringer gjentas, og hvor mange ganger?
> * Det er 100 bestillinger med til sammen ca. 250 linjer. Hvorfor ble det *ikke* én spørring per linje for å hente kaffebønnen? (Hint: persistence context / 1.-nivå-cache.)
> * Hva er det Hibernate gjør under panseret når du kaller `.getBrenneri()`? Skriv ut `kaffebonne.getBrenneri().getClass()` og se hva du får.
> * Alle `@ManyToOne` i domenet er eksplisitt satt til `fetch = FetchType.LAZY`. Fjern det på `Kaffebonne.brenneri`. Hva er standardverdien, og hva skjer med antall spørringer?
> * Hva skjer med ytelsen (og minnet) på fredag ettermiddag når det ligger 10 000 ordre i databasen?
> * Slå også på `org.hibernate.orm.jdbc.bind` (`TRACE`) i `logback.xml`. Hva ser du nå som du ikke så før? Mange eksempler bruker `hibernate.show_sql=true` i stedet for loggeren. Hvorfor er loggeren bedre i et ekte prosjekt?

---

## Nivå 2: Ta tilbake kontrollen
*Mål: Løse N+1-problematikken og skrive tester som sikrer ytelsen fremover.*

**Bakgrunn:**
Vi kan ikke ha et system som fyrer av over hundre spørringer for å hente 100 ordre. Vi må fortelle databasen at den skal gjøre den tunge løftingen for oss.

**Oppgave A: Én spørring**
1. Kjør `KvitteringTest`. Testen `kvitteringKreverBareEnSporring` er rød.
2. Endre `hentAlleBestillinger()` i `KaffebarRepository`. Bruk en eksplisitt `JOIN FETCH` som henter bestilling, linjer, bønner og brenneri i én og samme databaseoperasjon.
3. Verifiser i loggen at det nå kun går *én* SQL-spørring, og at det fortsatt kommer 100 bestillinger tilbake (ikke én per linje).
4. Prøv det samme med en `EntityGraph` i stedet for JOIN FETCH (`em.createEntityGraph(Bestilling.class)` og query-hintet `jakarta.persistence.fetchgraph`). Hva er fordelen med å skille *hva* du henter fra *hvordan* du henter det?

**Oppgave B: Tilpasninger og det kartesiske produktet**
Kundene kan nå legge til tilpasninger på bestillingen (havremelk, ekstra shot …). `Bestilling` har fått en ny liste: `List<Tilpasning> tilpasninger`.

1. Kjør `TilpasningTest`. Den er rød.
2. Implementer `hentBestillingerMedTilpasninger()` i `KaffebarRepository` og `lagKvitteringMedTilpasninger(...)` i `Kvitteringsskriver`.
3. Første forsøk: `JOIN FETCH` både `linjer` og `tilpasninger` i samme spørring. Hva skjer?
4. Finn en løsning som gir maks to spørringer. Det finnes flere; prøv gjerne mer enn én:
   * Hent de to listene i to separate spørringer (den andre spørringen fyller ut bestillingene som allerede ligger i persistence context).
   * Bytt fra `List` til `Set` (hva koster det?).
   * `@BatchSize` eller `hibernate.default_batch_fetch_size`.

**Oppgave C: Kassa-appen skriver**
Til nå har vi bare lest. Kassa-appen må også registrere bestillinger og fjerne linjer når kunden angrer. En kollega har skrevet `BestillingTjeneste`.

1. Kjør `KassaTest`. Begge testene er røde.
2. `registrer(...)` legger linjene inn med `bestilling.getLinjer().add(...)`. Les feilmeldingen. Hvilken side av relasjonen mellom `Bestilling` og `Bestillingslinje` er *eiersiden*, og hva betyr `mappedBy`? Få testen grønn, og sørg for at totalprisen blir riktig.
3. Eksperiment: fjern `cascade = CascadeType.ALL` fra `Bestilling.linjer` og kjør testen. Hva skjer, og hvorfor? Sett det tilbake.
4. `fjernLinje(...)` kaller `em.remove(linje)`. Testen viser at linjen *ikke* blir slettet, og det kommer ingen feilmelding. Hvorfor? (Hint: linjen ligger fortsatt i `bestilling.getLinjer()`, og samlingen har `cascade = ALL`.)
5. Lag en domenemetode `Bestilling.fjernLinje(...)` som holder både samlingen og totalprisen i orden, og bruk den. Hvilken rolle spiller `orphanRemoval = true`? Prøv å sette den til `false`.

> 🤔 **Refleksjon i par:**
> * Hva er faren ved å bruke `EAGER` fetching som standard på entitetene i stedet for `JOIN FETCH` i spørringen?
> * Hvorfor nekter Hibernate å JOIN FETCH-e to `List`-er samtidig, mens den godtar to `Set`? Hvor mange rader returnerer databasen for en bestilling med 4 linjer og 3 tilpasninger?
> * `Bestilling.fjernLinje` bruker `linjer.remove(linje)`, som bruker `equals`. Entitetene våre overstyrer ikke `equals`/`hashCode`. Hva ville skjedd med en `Set<Bestillingslinje>` hvis `hashCode` var basert på `id`, og linjen ble lagt i settet *før* den ble persistert?
> * Hvorfor bør `getLinjer()` egentlig returnere en liste som ikke kan endres utenfra?
> * Hvordan sikrer testen med `QueryCountLogger` at ingen innfører N+1 igjen om et halvt år?

---

## Nivå 3: Kampanjetabben (Dirty Checking)
*Mål: Forstå entitetenes livssyklus (Managed vs. Detached) og farene ved ufrivillige dataendringer.*

**Bakgrunn:**
Kaffebaren kjører en kampanje! Alle kunder som har kjøpt bønner fra «Tim Wendelboe», skal få 15 % rabatt. Denne rabatten gjelder kun for visning i en ny kampanje-app (frontend). Ordren skal *ikke* endres historisk i databasen.

**Oppgave A: Tabben**
En kollega har allerede skrevet `KampanjeTjeneste.genererKampanjeKvittering(Long bestillingId)`. Kollegaen har til og med vært flink og returnerer en `KampanjeKvittering` (en `record`) i stedet for entiteten. Metoden gjør dette:
1. Starter en transaksjon (`em.getTransaction().begin()`).
2. Henter bestillingen med ID-en.
3. Sjekker om den inneholder bønner fra riktig brenneri. Hvis ja: setter totalprisen på `Bestilling`-entiteten til kampanjeprisen.
4. Committer transaksjonen og bygger `KampanjeKvittering` fra entiteten.

Kjør `KampanjeTest`. Kampanjeprisen er riktig, men testen `kampanjenEndrerIkkeOrdrenIDatabasen` er rød. Den henter ordren på nytt i en ny `EntityManager` etter kampanjen. Koble også gjerne til databasen med DBeaver/pgAdmin mens appen kjører (tilkoblingsinfo skrives ut ved oppstart), og kall http://localhost:8080/kampanje/1 noen ganger.

**Vent nå litt … Ble prisen endret i databasen?!** Kollegaen kalte jo aldri `em.merge()` eller `em.persist()`! Og hvorfor hjalp det ikke at metoden returnerer en DTO?

Rett opp `genererKampanjeKvittering` slik at den aldri endrer en managed entitet, og få `KampanjeTest` grønn.

**Oppgave B: Entiteter i API-et**
Kassa-appen trenger `GET /bestillinger/{id}`. Kollegaen har laget `BestillingResource`, som returnerer `Bestilling`-entiteten direkte.
1. Kjør `BestillingApiTest`, eller start appen og åpne http://localhost:8080/bestillinger/1. Les feilmeldingen i loggen. Hvorfor er `EntityManager` lukket når Jackson serialiserer?
2. Prøv den «åpenbare» løsningen: JOIN FETCH alt i ressursen og returner fortsatt entiteten. Hva skjer nå? (Hint: `Bestillingslinje.getBestilling()`.)
3. Gjør det riktig: lag en `BestillingDto` (gjerne `record`-er) som har formatet beskrevet i javadoc-en til `BestillingResource`, og map til den mens `EntityManager` er åpen. Tenk på det du lærte på nivå 2: hvor mange spørringer bruker endepunktet?

**Oppgave C (bonus): Stengetid**
Ved stengetid skal alle bestillinger som står klare, markeres som hentet, og kundene får 10 kr i kompensasjon. Kollegaen har skrevet `StengetidTjeneste.stengKassen()`. Den bruker en JPQL-`UPDATE` for å endre status på alle bestillingene i én operasjon.
1. Kjør `StengetidTest`. Kompensasjonen er gitt, men statusen er fortsatt `KLAR`. Les koden og SQL-loggen. Hvor ble statusen satt tilbake?
2. Hva vet persistence context om en JPQL-`UPDATE` (eller `DELETE`)?
3. Få testen grønn. Det finnes flere løsninger: bare bulk, bare entiteter, eller `em.clear()`/`em.refresh()` på riktig sted. Hva er fordelene og ulempene?

**Oppgave D (bonus): Den motsatte fellen**
Kassa-appen henter en bestilling, endrer status og sender den inn igjen til `BestillingTjeneste.lagre(Bestilling)`. Kollegaen har lært av oppgave A og tenker at «Hibernate oppdager endringer automatisk».
1. Kjør `FrakobletEntitetTest`. Ble endringen lagret?
2. Få testen grønn. Hva er forskjellen på en *managed* og en *detached* entitet?
3. Hva returnerer `em.merge()`, og hvorfor er det viktig? Hvilke felt skriver `merge()` tilbake, og er det alltid det du vil?

> 🤔 **Refleksjon i par:**
> * Hvorfor lagret Hibernate objektet ditt uten at du ba om det? Hva betyr det at en entitet er «managed», og når skjer flush?
> * Kollegaen returnerte en DTO, men skadet databasen likevel. Hva er det egentlig DTO-en beskytter mot, og hva beskytter den *ikke* mot?
> * Sammenlign løsningene: DTO/projeksjon (`SELECT new ...`), `em.detach()`, og read-only-hintet `org.hibernate.readOnly`. Hvilken velger du, og hvorfor?
> * Hvorfor er det en dårlig idé å la forretningslogikk (som rabattberegning) jobbe direkte på managed entiteter?
> * Hvorfor er det en dårlig idé å eksponere entiteter direkte i et REST-API, selv når det «fungerer»?
> * Hibernate skriver som standard *alle* kolonner i en `UPDATE`. Hva ville `@DynamicUpdate` endret i stengetid-oppgaven, og hvorfor er det likevel ikke en ekte løsning?

---

## Nivå 4: Kollisjoner og rå kraft
*Mål: Håndtere samtidighet med versjonering, og vite når man skal gå forbi ORM-et.*

**Oppgave A: Kollisjonen (Optimistic Locking)**
To baristaer prøver å markere den samme ordren som «Hentet» på nøyaktig samme tidspunkt.
1. Kjør `KollisjonTest`. Den simulerer to baristaer med hver sin `EntityManager`, steg for steg (ingen tråder, så testen er forutsigbar): begge henter samme ordre, begge endrer status, og begge committer. Testen forventer at den andre committen feiler, og er rød. Hvem «vinner» i dag?
2. Utvid `Bestilling` med et versjonsfelt (`@Version private Long versjon;`) og få testen grønn. Se på `UPDATE`-setningen i loggen. Hva har endret seg? Slå gjerne på `org.hibernate.orm.jdbc.bind` i `logback.xml` for å se versjonsnummeret som sendes med.
3. Kollisjoner skjer også *mellom* HTTP-kall: baristaen ser bestillingen på skjermen, og noen andre endrer den før baristaen rekker å trykke «Hentet». Endepunktet `PUT /bestillinger/{id}/hentet` tar derfor imot versjonen klienten så, i headeren `If-Match`. `BestillingTjeneste.markerSomHentet` sammenligner den med versjonen i databasen.
4. Kjør `KollisjonApiTest`. Etter steg 2 får første barista `204`, men andre barista får `500 Internal Server Error`. Lag en `ExceptionMapper` som oversetter konflikten til en fornuftig HTTP-statuskode (testen godtar `409` og `412`) og registrer den i `KaffebarApp`.
5. Bonus: la `GET /bestillinger/{id}` sende versjonen som `ETag`, så klienten vet hva den skal sende i `If-Match`.

> 🤔 **Refleksjon i par:**
> * Hvilken HTTP-statuskode valgte du, `409 Conflict` eller `412 Precondition Failed`, og hvorfor?
> * Når konflikten oppdages ved commit (steg 2), får du ikke en `OptimisticLockException` direkte. Hva får du, og hva betyr det for mapperen din?
> * Bør backend automatisk prøve på nytt (retry), eller er det klientens ansvar? Gjelder svaret ditt også for «sett status til HENTET»?
> * Når ville du valgt pessimistisk låsing (`LockModeType.PESSIMISTIC_WRITE`) i stedet?

**Oppgave B: Den komplekse lesemodellen**
Ledelsen krever et nytt dashboard: *De 3 mest solgte bønnetypene per brenneri per måned, med totalomsetning.*

Å laste tusenvis av entiteter inn i minnet for å aggregere i Java er feil verktøy for jobben. Her skal databasen jobbe.

1. Kjør `RapportTest`. Den er rød. Fasiten i testen er regnet ut på den «naive» måten: alle linjene lastes inn som entiteter og aggregeres i Java. Se hvor mye kode det ble.
2. Skriv SQL-en i `RapportRepository`. Reglene står i javadoc-en. Tips: se på tabellene og kolonnene Hibernate har laget i loggen eller i DBeaver. Merk: `GROUP BY` + `LIMIT` holder *ikke*, fordi `LIMIT` begrenser hele resultatet, ikke hver gruppe. Du trenger en vindusfunksjon: `ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...)`.
3. Kjør SQL-en på to måter og map resultatet til `RapportRad` (en `record`):
   * `toppBonnerMedNativeQuery()`: `em.createNativeQuery(sql, Tuple.class)`
   * `toppBonnerMedJdbc()`: Hibernates «bakdør» til den underliggende JDBC-tilkoblingen:
     ```java
     return em.unwrap(Session.class).doReturningWork(connection -> {
         try (PreparedStatement stmt = connection.prepareStatement("DIN RÅ SQL HER")) {
             // Map resultatet direkte til RapportRad
         }
     });
     ```
4. Bonustesten `RapportFlushTest` legger inn en ny bestilling i samme transaksjon (uten commit) og kjører deretter rapporten. Hvilken av de to variantene ser den nye bestillingen, og hvorfor? Få begge grønne.

> 🤔 **Refleksjon i par:**
> * Hva mistet du ved å gå rett på JDBC med `doWork`? (Tenk flush, typekonvertering og persistence context.)
> * Hibernate 6+ støtter vindusfunksjoner og CTE-er i HQL. Når er native SQL likevel riktig valg?
> * Hvor går grensen for når et lesebehov bør løses med entiteter, med DTO-projeksjon i JPQL, eller med native SQL?

**Oppgave C (bonus): Black Friday-importen**
Nettbutikken sender 1000 bestillinger med 2 linjer hver i én fil. `ImportTjeneste.importer(...)` persisterer dem én etter én.
1. Kjør `ImportTest`. Hvor mange INSERT-setninger forbereder Hibernate, og hvor lang tid tar det?
2. Slå på JDBC-batching i `persistence.xml` med `hibernate.jdbc.batch_size` (prøv 50). Kjør testen igjen. Hvorfor ble det bare litt bedre? (Hint: i hvilken rekkefølge kommer INSERT-ene i loggen?)
3. Finn innstillingen som lar Hibernate sortere INSERT-ene per tabell, og få testen grønn.
4. Alle 3000 entitetene ligger i persistence context til commit, sammen med et øyeblikksbilde av hver entitet for dirty checking. Gjør importen minnevennlig med `em.flush()` og `em.clear()` med jevne mellomrom. Hva må du passe på med kaffebønnene etter `em.clear()`?

`QueryCountLogger` teller hvor mange ganger Hibernate *forbereder* en SQL-setning. Med batching gjenbrukes én forberedt INSERT for mange rader, så tallet blir lavere enn antall rader.

> 🤔 **Refleksjon i par:**
> * Entitetene våre bruker `@GeneratedValue`, som gir `SEQUENCE` på PostgreSQL. Hva skjer med batchingen hvis du bytter til `GenerationType.IDENTITY`, og hvorfor?
> * Hva gjør `allocationSize` på en sekvens, og hvorfor ser du så få `select nextval(...)` i loggen?
> * Når ville du droppet JPA helt for en import, og brukt for eksempel `COPY` i PostgreSQL?
