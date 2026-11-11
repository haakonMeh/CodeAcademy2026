# Nivå 1: Den naive kvitteringen

Oppgaveteksten står i [`../oppgaver.md`](../oppgaver.md). Her starter du. Ingen nivåer er løst ennå.
Oppsett og container-runtime står i [`../README.md`](../README.md).

## Kom i gang

```bash
dotnet tool restore
dotnet test                 # 2 tester: 2 røde
dotnet test --filter Level1A --logger "console;verbosity=detailed"   # én oppgave, med SQL-en synlig
```

## Hvor skal jeg jobbe?

Filene ligger i `Kaffebar.Orm/`.

| Oppgave | Fil | Filter |
| --- | --- | --- |
| 1A | `KaffebarRepository.Level1A.cs`, `Receipts.cs` | `Level1A` |
| 1B | `Data/KaffebarDb.cs` | `Level1B` |
