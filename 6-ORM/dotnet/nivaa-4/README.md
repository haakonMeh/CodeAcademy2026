# Nivå 4: Kollisjoner og rå kraft

Oppgaveteksten står i [`../oppgaver.md`](../oppgaver.md). Denne mappen inneholder løsningen på nivå 3.
Oppsett og container-runtime står i [`../README.md`](../README.md).

## Kom i gang

```bash
dotnet tool restore
dotnet test                 # 24 tester: 17 grønne, 7 røde
dotnet test --filter Level4A --logger "console;verbosity=detailed"   # én oppgave, med SQL-en synlig
```

## Hvor skal jeg jobbe?

Filene ligger i `Kaffebar.Orm/`.

| Oppgave | Fil | Filter |
| --- | --- | --- |
| 4A | `Domain/Order.cs`, ny migrering, `KaffebarRepository.Level4A.cs` | `Level4A` |
| 4B | `KaffebarRepository.Level4B.cs` | `Level4B` |
| 4C (bonus) | `KaffebarRepository.Level4C.cs` | `Level4C` |
