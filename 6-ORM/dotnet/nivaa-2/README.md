# Nivå 2: Ta tilbake kontrollen

Oppgaveteksten står i [`../oppgaver.md`](../oppgaver.md). Denne mappen inneholder løsningen på nivå 1.
Oppsett og container-runtime står i [`../README.md`](../README.md).

## Kom i gang

```bash
dotnet tool restore
dotnet test                 # 7 tester: 2 grønne, 5 røde
dotnet test --filter Level2A --logger "console;verbosity=detailed"   # én oppgave, med SQL-en synlig
```

## Hvor skal jeg jobbe?

Filene ligger i `Kaffebar.Orm/`.

| Oppgave | Fil | Filter |
| --- | --- | --- |
| 2A | `KaffebarRepository.Level2A.cs` | `Level2A` |
| 2B | `KaffebarRepository.Level2B.cs` | `Level2B` |
| 2C | `KaffebarRepository.Level2C.cs`, `Domain/Order.cs` | `Level2C` |
