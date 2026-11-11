# Nivå 4 – Kollisjoner og rå kraft

Oppgaveteksten står i `../oppgaver.md`. Denne mappen inneholder løsningen på nivå 3.

## Forutsetninger
* Java 25 (`java -version`)
* Maven 3.9+
* Docker kjører (Docker Desktop, Colima, Rancher, Podman …)

## Kom i gang
```bash
mvn test                       # kjører alle testene (flere er røde til du har løst oppgavene)
mvn test -Dtest=KollisjonTest
mvn compile exec:java          # starter appen på http://localhost:8080
```

Prøv kollisjonen for hånd mens appen kjører:
```bash
curl -i -X PUT -H 'If-Match: "0"' http://localhost:8080/bestillinger/1/hentet
```

## Logging
All logging styres fra `src/main/resources/logback.xml`. SQL-setningene er slått på. Sett `org.hibernate.orm.jdbc.bind` til `TRACE` for å se parameterverdiene.

## Hvor skal jeg jobbe?
| Oppgave | Fil | Test |
|---|---|---|
| A – Kollisjonen | `domene/Bestilling.java`, ny `ExceptionMapper` i `api/`, `KaffebarApp.java` | `KollisjonTest`, `KollisjonApiTest` |
| B – Lesemodellen | `rapport/RapportRepository.java` | `RapportTest` |
| B – Bonus | `rapport/RapportRepository.java` | `RapportFlushTest` |
| C – Importen (bonus) | `importering/ImportTjeneste.java`, `persistence.xml` | `ImportTest` |

## Feilsøking: «Could not find a valid Docker environment» (Colima)
```bash
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
```
