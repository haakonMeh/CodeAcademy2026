# 🎤 Presentasjoner for samling 5

To webbaserte presentasjoner laget med [reveal.js](https://revealjs.com/). Hver del er ca. 30 minutter og leder inn til workshopen.

| Del | Fil | Leder inn til |
|---|---|---|
| 1 · Context engineering og AI harness | `del1-harness.html` | [`oppgaver-harness.md`](../oppgaver-harness.md) |
| 2 · Subagenter, orkestrering og guardrails | `del2-orkestrering.html` | [`oppgaver-orkestrering.md`](../oppgaver-orkestrering.md) |

## Kjør lokalt

reveal.js lastes fra CDN, så du trenger nettilgang. Start en enkel webserver i denne mappen:

```bash
cd 5-AI/presentasjon
python3 -m http.server 8000
```

Åpne <http://localhost:8000>.

## Tastatursnarveier

| Tast | Funksjon |
|---|---|
| `→` / `Space` | Neste steg (animasjoner) |
| `S` | Talernotater med tidsforslag |
| `F` | Fullskjerm |
| `O` / `Esc` | Oversikt over alle slides |
| `T` | Spill av terminal-animasjonen på nytt |
| `Alt` + klikk | Zoom inn |

## Interaktive elementer

- **Quiz-slides:** Klikk på et kort for å avsløre svaret. La salen gjette først.
- **Workshop-timer:** Siste slide i hver del har en 60-minutters nedtelling.
