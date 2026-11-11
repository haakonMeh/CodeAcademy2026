# Kaffebar ORM – .NET 10 / EF Core 10

Oppsettet for samling 6. Se [`oppgaver.md`](./oppgaver.md) for oppgavene.

Ingen web-API, ingen DI og ingen compose-oppsett: et klassebibliotek med EF Core, og et
testprosjekt som starter en ekte Postgres for deg med Testcontainers.

## Før samlingen

Gjør dette hjemme, før samlingen. Det tar et kvarter, og da kan du begynne på oppgavene med en gang
presentasjonen er ferdig.

1. **Installer .NET 10 SDK** fra [dot.net](https://dotnet.microsoft.com/download). Sjekk: `dotnet --version`
   skal vise `10.x`.
2. **Ha en container-runtime som kjører.** Testene starter sin egen Postgres i en container. Har du allerede
   Docker Desktop, Rancher Desktop eller Colima, holder det å starte den. Ellers anbefaler vi Podman:
   - **macOS:** `brew install podman`, så `podman machine init` og `podman machine start`. Eller installer
     [Podman Desktop](https://podman-desktop.io), som setter opp maskinen for deg.
   - **Windows:** installer [Podman Desktop](https://podman-desktop.io) og følg veiviseren. Den bruker WSL2.
   - **Linux:** installer `podman` fra pakkebrønnen din.

   Sjekk: `podman info` (eller `docker info`) svarer uten feil.
3. **Ha en editor med C#-støtte:** Rider, Visual Studio, eller VS Code med C# Dev Kit.
4. **Kjør testene én gang.** Det laster ned Postgres-imaget, så det går fort på samlingen:

   ```bash
   cd 6-ORM/dotnet/nivaa-1
   dotnet tool restore
   dotnet test
   ```

   **Du er klar** når begge testene feiler med `NotImplementedException : Oppgave 1A`. De skal være røde.
   Det er oppgavene.

   **Ikke klar** hvis du får `DockerUnavailableException`, eller noe om at Docker ikke kjører. Da finner ikke
   testene noen container-runtime. Sjekk at den er startet (Podman: `podman machine start`), og se
   [Hvilken container-runtime blir brukt?](#hvilken-container-runtime-blir-brukt). Får du det ikke til, ta det
   opp med kursholderen før samlingen.

Bruker du Podman, starter maskinen ikke alltid av seg selv etter en omstart av PC-en. Kjør
`podman machine start` på samlingsdagen hvis testene ikke finner den.

## Kjør prosjektet

Hvert nivå er et eget prosjekt. Gå til mappen for nivået du skal jobbe med:

```bash
cd nivaa-1
dotnet tool restore     # henter dotnet-ef lokalt (dotnet-tools.json)
dotnet test             # starter Postgres, migrerer, seeder, kjører testene
```

Første kjøring laster ned `postgres:18-alpine`. README-en i hver mappe sier hvor mange grønne og røde
tester du skal starte med. Det er de røde du skal gjøre grønne.

Vil du se SQL-en, kvitteringene og antall spørringer, filtrerer du på en oppgave og skrur opp
detaljnivået:

```bash
dotnet test --filter Level1 --logger "console;verbosity=detailed"
```

Test-runneren i IDE-en din viser den samme outputen per test.

## Hvilken container-runtime blir brukt?

`Kaffebar.Orm.Tests/ContainerRuntime.cs` leter i denne rekkefølgen og bruker den første som
faktisk svarer:

| # | Runtime | Hvor |
| --- | --- | --- |
| 1 | Det du har satt selv | miljøvariabelen `DOCKER_HOST` (vinner alltid) |
| 2 | Podman machine (macOS/Windows) | en maskin som **kjører** i `podman machine list`, og socketen dens (named pipe på Windows) |
| 3 | Podman (Linux) | `$XDG_RUNTIME_DIR/podman/podman.sock` |
| 4 | Docker Desktop | `~/.docker/run/docker.sock` |
| 5 | Rancher Desktop | `~/.rd/docker.sock` |
| 6 | Colima | `~/.colima/default/docker.sock` |
| 7 | Standard | `/var/run/docker.sock` |

Hver socket testes med en ekte tilkobling, så en socket-fil som er blitt liggende etter at runtimen
ble stoppet, hoppes over. Det samme gjelder en stoppet Podman-maskin. Svarer ingen, får
Testcontainers bruke sine egne standarder (på Windows: Docker Desktop sin named pipe).

Testen `Level1A_…` skriver øverst hvilken som ble valgt («Kjørt mot …»), også før den er grønn:
`dotnet test --filter Level1A --logger "console;verbosity=detailed"`.

- **Podman:** Ryuk (Testcontainers sin opprydder) skrus av automatisk, fordi den kjører dårlig på
  Podman. Containerne ryddes når testklassen er ferdig, men **ikke** hvis du avbryter en kjøring
  eller stopper debuggeren midt i. Da blir de liggende: se etter dem med `podman ps` og fjern dem
  med `podman rm -f <id>`. Sørg for at maskinen kjører: `podman machine start`.
- **Rancher Desktop:** må kjøre med **dockerd (moby)** som container engine, ikke containerd. Med
  containerd finnes det ingen Docker-socket (*Preferences → Container Engine*).
- **Colima:** `colima start` før du kjører testene.
- **Finner den feil runtime?** Sett `DOCKER_HOST` selv, f.eks.
  `export DOCKER_HOST=unix://$HOME/.colima/default/docker.sock`.

### Ingen containere? Bruk din egen Postgres

Kan du ikke kjøre Testcontainers, kan du peke testene på en Postgres du har selv. Fixturen lager da
en egen database per testklasse og sletter den etterpå, så brukeren må kunne `CREATE DATABASE`:

```bash
podman run -d --name kaffebar-pg -p 5432:5432 -e POSTGRES_PASSWORD=postgres docker.io/library/postgres:18-alpine
# eller: docker run -d --name kaffebar-pg -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:18-alpine

export KAFFEBAR_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=postgres"
dotnet test
```

PowerShell:

```powershell
$env:KAFFEBAR_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=postgres"
dotnet test
```

## Struktur

```
6-ORM/dotnet/
├── oppgaver.md                           # oppgaveteksten
├── README.md
├── nivaa-1/ … nivaa-4/                   # ett selvstendig prosjekt per nivå
│   ├── KaffebarOrm.slnx
│   ├── dotnet-tools.json                 # dotnet-ef, låst til samme versjon som EF Core
│   ├── Kaffebar.Orm/                     # klassebiblioteket, her jobber du
│   │   ├── Domain/                       # Order, OrderLine, Bean, Roastery, StatusChange, enums
│   │   ├── Data/
│   │   │   ├── KaffebarDbContext.cs      # modellen (Fluent API)
│   │   │   ├── KaffebarDb.cs             # konfigurasjon: Npgsql, logging, lazy loading
│   │   │   ├── QueryCounter.cs           # teller spørringer og rader, trenger ikke endres
│   │   │   ├── SeedData.cs               # 6 brennerier, 15 bønner, 100 ordrer (deterministisk)
│   │   │   └── DesignTimeDbContextFactory.cs # for dotnet ef
│   │   ├── Migrations/
│   │   ├── KaffebarRepository.cs         # det som er ferdig: UpdateStatusAsync, GetOrderAsync
│   │   └── KaffebarRepository.Level*.cs  # én fil per oppgave
│   └── Kaffebar.Orm.Tests/
│       ├── PostgresFixture.cs            # én migrert og seedet Postgres per testklasse
│       ├── ContainerRuntime.cs           # finner Podman/Docker/Rancher/Colima
│       └── Level*Tests.cs                # testene for nivået, og for nivåene før
└── losninger/                            # kun for kursholder, med FASIT.md
```

`KaffebarRepository` er en `partial class` med én fil per oppgave, så hver oppgave står for seg selv.

## Om dataene

Brenneriene finnes (og lager god kaffe), men bønnene, prisene og salgstallene er oppdiktet. Menyen
og kundene Ada, Kari, Jonas og Ingrid er de samme som i samling 3.

Lykke til, og god kaffe! ☕
