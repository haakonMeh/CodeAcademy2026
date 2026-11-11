# Nivå 3: Kampanjetabben

Oppgaveteksten står i [`../oppgaver.md`](../oppgaver.md). Denne mappen inneholder løsningen på nivå 2.
Oppsett og container-runtime står i [`../README.md`](../README.md).

## Kom i gang

```bash
dotnet tool restore
dotnet test                 # 16 tester: 8 grønne, 8 røde
dotnet test --filter Level3A --logger "console;verbosity=detailed"   # én oppgave, med SQL-en synlig
```

## Hvor skal jeg jobbe?

Filene ligger i `Kaffebar.Orm/`.

| Oppgave | Fil | Filter |
| --- | --- | --- |
| 3A | `KaffebarRepository.Level3A.cs`, `Campaign.cs` | `Level3A` |
| 3B | `KaffebarRepository.Level3B.cs` | `Level3B` |
| 3C (bonus) | `KaffebarRepository.Level3C.cs` | `Level3C` |
| 3D (bonus) | `KaffebarRepository.Level3D.cs` | `Level3D` |
