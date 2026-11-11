# Nivå 3 – Kampanjetabben

Oppgaveteksten står i `../oppgaver.md`. Denne mappen inneholder løsningen på nivå 2.

## Forutsetninger
* Java 25 (`java -version`)
* Maven 3.9+
* Docker kjører (Docker Desktop, Colima, Rancher, Podman …)

## Kom i gang
```bash
mvn test                       # kjører alle testene (flere er røde til du har løst oppgavene)
mvn test -Dtest=KampanjeTest
mvn compile exec:java          # starter appen på http://localhost:8080
```

Endepunkter når appen kjører:
* http://localhost:8080/kvitteringer
* http://localhost:8080/bestillinger/1
* http://localhost:8080/kampanje/1

Tilkoblingsinfo til Postgres skrives ut ved oppstart, så du kan følge med i DBeaver/pgAdmin.

## Logging
All logging styres fra `src/main/resources/logback.xml`. SQL-setningene er slått på. Sett `org.hibernate.orm.jdbc.bind` til `TRACE` for å se parameterverdiene.

## Hvor skal jeg jobbe?
| Oppgave | Fil | Test |
|---|---|---|
| A – Tabben | `tjeneste/KampanjeTjeneste.java` | `KampanjeTest` |
| B – Entiteter i API-et | `api/BestillingResource.java` (+ en ny DTO) | `BestillingApiTest` |
| C – Stengetid (bonus) | `tjeneste/StengetidTjeneste.java` | `StengetidTest` |
| D – Den motsatte fellen (bonus) | `tjeneste/BestillingTjeneste.java` → `lagre(...)` | `FrakobletEntitetTest` |

`api/UventetFeilMapper` logger uventede feil fra REST-kall med full stack trace. Les loggen!

## Feilsøking: «Could not find a valid Docker environment» (Colima)
```bash
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
```
