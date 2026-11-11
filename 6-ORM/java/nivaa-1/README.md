# Nivå 1 – Den naive kvitteringen

Oppgaveteksten står i `../oppgaver.md`.

## Forutsetninger
* Java 25 (`java -version`)
* Maven 3.9+
* Docker kjører (Docker Desktop, Colima, Rancher, Podman …)

## Kom i gang
```bash
mvn test                 # kjører KvitteringTest (rød til du har løst oppgaven)
mvn compile exec:java    # starter appen på http://localhost:8080/kvitteringer
```
Første kjøring laster ned Postgres-imaget og tar litt tid.

Databasen startes automatisk med Testcontainers og fylles med 5 brennerier,
15 kaffebønner og 100 bestillinger (`db/TestData.java`). Tilkoblingsinfo skrives
ut ved oppstart, så du kan koble til med DBeaver/pgAdmin mens appen kjører.

## Hvor skal jeg jobbe?
| Fil | Hva |
|---|---|
| `repository/KaffebarRepository.java` | `hentAlleBestillinger()` – TODO |
| `kvittering/Kvitteringsskriver.java` | `lagKvittering(...)` – TODO |
| `src/main/resources/logback.xml` | SQL-logging (`org.hibernate.SQL`) og parameterverdier |
| `src/test/java/.../KvitteringTest.java` | Testen du kjører |

`db/QueryCountLogger` teller SQL-setningene Hibernate sender. Testen og endepunktet skriver ut antallet.

## Feilsøking: «Could not find a valid Docker environment» (Colima)
```bash
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
```
