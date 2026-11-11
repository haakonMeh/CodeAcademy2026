# Kaffebaren – Code Academy: ORM og Hibernate

Hei, og velkommen! 👋

Dette er en kveld for å se nærmere på hva som egentlig skjer når Java-objekter blir til rader i en database. Vi bruker ren Hibernate, Jersey og PostgreSQL, uten Spring. Du trenger ikke kunne Hibernate fra før. Det viktigste er at du er nysgjerrig og har lyst til å kode litt.

Denne siden handler om å bli klar **før** workshopen, så du kan bruke kvelden på det morsomme og ikke på installasjon. Regn med 15–30 minutter, mest ventetid mens ting lastes ned.

---

## Hva du trenger

| Verktøy | Versjon | Sjekk med |
|---|---|---|
| Java (JDK) | 25 eller nyere | `java -version` |
| Maven | 3.9 eller nyere | `mvn -version` |
| Docker | Nyere versjon som kjører | `docker info` |
| En IDE | IntelliJ IDEA, VS Code e.l. | – |

Du trenger **ikke** installere PostgreSQL selv. Databasen startes automatisk i en Docker-container når du kjører testene eller appen.

### Java 25
Det enkleste er ofte [SDKMAN!](https://sdkman.io/) (macOS/Linux):

```bash
sdk install java 25-tem
```

Alternativer: `brew install --cask temurin@25` på macOS, eller last ned Temurin fra [adoptium.net](https://adoptium.net/) (fungerer også på Windows). Sjekk at `java -version` viser 25. Har du flere JDK-er, må `JAVA_HOME` peke på riktig.

### Maven
```bash
sdk install maven        # eller: brew install maven
```

### Docker
Alt som gir deg en fungerende `docker`-kommando, går fint: Docker Desktop, Colima, Rancher Desktop eller Podman. Det viktigste er at den **kjører** når du skal jobbe. `docker info` skal svare uten feilmelding.

### IDE
Bruk det du liker. IntelliJ IDEA (Community holder) åpner prosjektene rett fra `pom.xml`. Velg «Open» på mappen for nivået du skal jobbe med, for eksempel `nivaa-1`.

---

## Gjør dette før workshopen

Det tar tid å laste ned avhengigheter og Postgres-imaget første gang, og nettet kan være tregt når mange gjør det samtidig. Derfor er det lurt å gjøre det hjemme eller på kontoret i forkant.

1. **Hent koden** og gå inn i mappen.

2. **Hent Postgres-imaget:**
   ```bash
   docker pull postgres:18-alpine
   ```

3. **Last ned avhengighetene** for nivåene. Det er lurt å ta alle fire, siden du kanskje vil hoppe videre i løpet av kvelden:
   ```bash
   for n in 1 2 3 4; do (cd nivaa-$n && mvn -q test-compile); done
   ```

4. **Kjør en prøvetur:**
   ```bash
   cd nivaa-1
   mvn test
   ```
   Testene skal feile! 🔴 Det er meningen. Oppgavene i kveld går ut på å gjøre dem grønne. Det viktige nå er at Maven kompilerer, at en database starter og at testene faktisk *kjører*. Får du en feil om Docker eller Java-versjon, se feilsøkingen nedenfor.

5. **Bonus:** Start appen og se at den svarer:
   ```bash
   mvn compile exec:java
   ```
   Åpne <http://localhost:8080/kvitteringer>. Stopp med `Ctrl+C`.

Kom du hit uten feilmeldinger, er du klar. ☕

---

## Litt om hvordan kvelden er lagt opp

- Oppgavene står i [oppgaver.md](oppgaver.md). Du trenger ikke lese dem på forhånd, men ta gjerne en titt.
- Oppgavene er delt i fire nivåer, og hvert nivå er et eget Maven-prosjekt (`nivaa-1` … `nivaa-4`). Mappen for et nivå inneholder løsningen på nivået før, så du kan starte der det passer deg.
- Du velger selv hvor du begynner:
  - **Ny på Hibernate?** Nivå 1 og 2.
  - **Erfaren, men rusten?** Nivå 2 og 3.
  - **ORM-veteran?** Nivå 3 og 4.
- Vi jobber gjerne i par. Refleksjonsspørsmålene er minst like viktige som å få testene grønne.
- Det er helt greit å ikke bli ferdig. Målet er å forstå, ikke å rekke alt.

Hvis du vil friske opp noe på forhånd (helt valgfritt):
- Grunnleggende SQL: `SELECT`, `JOIN`, `WHERE`
- Hva en `@Entity`, `@ManyToOne` og `@OneToMany` er i JPA

---

## Feilsøking

**«Could not find a valid Docker environment»**
Docker kjører ikke, eller Testcontainers finner den ikke. Sjekk `docker info`. Bruker du Colima, sett:
```bash
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
```

**Maven klager på Java-versjon («requires Java 25»)**
Maven bruker en eldre JDK. Sjekk hvilken Java `mvn -version` viser, og sett `JAVA_HOME` til JDK 25.

**Port 8080 er opptatt**
Noe annet kjører allerede på maskinen. Stopp det, eller lukk en tidligere instans av appen.

**Firmamaskin og proxy/Maven-speil**
Hvis Maven ikke får lastet ned avhengigheter, kan det skyldes proxy eller et internt Maven-speil i `~/.m2/settings.xml`. Prøv på et annet nett eller spør oss før workshopen.

Står du fast? Ta kontakt, så finner vi ut av det sammen. Det er mye bedre å fikse det før workshopen enn i starten av den.

Vi ses! ☕
