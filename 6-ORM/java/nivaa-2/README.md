# Nivå 2 – Ta tilbake kontrollen

Oppgaveteksten står i `../oppgaver.md`. Denne mappen inneholder løsningen på nivå 1.

## Forutsetninger
* Java 25 (`java -version`)
* Maven 3.9+
* Docker kjører (Docker Desktop, Colima, Rancher, Podman …)

## Kom i gang
```bash
mvn test                 # kjører alle testene (flere er røde til du har løst oppgavene)
mvn test -Dtest=KvitteringTest
mvn compile exec:java    # starter appen på http://localhost:8080/kvitteringer
```

Databasen startes automatisk med Testcontainers og fylles med 5 brennerier,
15 kaffebønner, 100 bestillinger og ca. 150 tilpasninger (`db/TestData.java`).
SQL-logging er slått på i `logback.xml` (loggeren `org.hibernate.SQL`). Der kan du også slå på parameterverdiene.

## Hvor skal jeg jobbe?
| Oppgave | Fil | Test |
|---|---|---|
| A – Én spørring | `repository/KaffebarRepository.java` → `hentAlleBestillinger()` | `KvitteringTest` |
| B – Tilpasninger | `KaffebarRepository.hentBestillingerMedTilpasninger()` og `Kvitteringsskriver.lagKvitteringMedTilpasninger(...)` | `TilpasningTest` |
| C – Kassa-appen skriver | `tjeneste/BestillingTjeneste.java`, `domene/Bestilling.java` | `KassaTest` |

`db/QueryCountLogger` teller SQL-setningene Hibernate sender. Testene bruker den til å sjekke antall spørringer.

## Feilsøking: «Could not find a valid Docker environment» (Colima)
```bash
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
```
