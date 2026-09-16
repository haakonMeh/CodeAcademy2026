# Anbefalt frontend-oppsett — Code Academy 2026

Dette er anbefalingene fra samling 4, samlet ett sted så du har noe å peke på mandag
morgen. De er skrevet for utviklere som ikke har frontend som hovedspråk, og for
tech leads som skal vurdere et oppsett noen andre har laget.

Anbefalingene er ikke nøytrale. Der det finnes et vanlig valg og et bedre valg, står det
bedre valget her, med begrunnelsen ved siden av.

---

## 1. Hva velger du til et nytt prosjekt?

| Situasjonen | Valget | Hvorfor |
| --- | --- | --- |
| Innholdsdrevet nettsted, mest statisk, SEO betyr noe | **Astro** | Sender null JavaScript som standard. Høyest tilfredshet i State of JS fem år på rad |
| App bak innlogging, internt verktøy, dashboard | **React + Vite + TanStack Router + TanStack Query** som SPA | Ingen server å drifte, enkleste mentale modell. *Dette er det vanligste tilfellet hos oss, og der folk oftest overkompliserer* |
| Offentlig produkt med både SEO, innlogging og serverlogikk | **Next.js (App Router)**, eventuelt TanStack Start | Du trenger faktisk serversiden |
| Tungt i .NET/Java fra før, trenger bare litt interaktivitet | **Server-rendret HTML med islands** (htmx, Alpine) | Et fullt legitimt valg i 2026. Ikke bygg en SPA for tre skjemaer |

Det interessante valget i 2026 er **hvor mye rammeverk du trenger**, ikke hvilket.

---

## 2. Standard-stacken

| Rolle | Valg | Merknad |
| --- | --- | --- |
| Språk | TypeScript, `strict` | Ikke til diskusjon |
| Bygg | Vite 8 (Rolldown) | Turbopack hvis Next |
| UI | React 19 | React Compiler er stabil — slutt å `useMemo` alt |
| Ruting | TanStack Router / React Router v7 | Filbasert i Next |
| Server-state | **TanStack Query** | Den enkeltendringen som gir mest |
| Klient-state | `useState` først, Zustand hvis du må | Redux er ikke default lenger |
| Skjema | React Hook Form + Zod | Samme Zod-skjema kan validere på server |
| Styling | CSS Modules **eller** Tailwind | Velg én |
| Komponenter | shadcn/ui eller Radix | Eie koden fremfor å arve et designsystem |
| Test | Vitest + Testing Library + Playwright | Ikke Jest i nye prosjekter |
| Mocking | MSW | Samme mock i test og i utvikling |
| Lint/format | Biome, eller ESLint flat config + Prettier | |
| API-klient | Generert fra OpenAPI | ← den samme kontrakten som driver backend |
| Auth | **BFF med HttpOnly-cookie** | Se regel 6 |

---

## 3. Åtte regler

1. **Server-state er ikke klient-state.** Data som egentlig bor i en database er en
   cache i frontenden, ikke en tilstand du eier. Ikke bland dem. Dette er den vanligste
   arkitekturfeilen i frontend.
2. **Ikke hent data i `useEffect`.** Bruk noe som kan caching, retry, dedupe og
   invalidering. Du kommer til å trenge alle fire.
3. **Typene skal komme fra kontrakten**, ikke skrives for hånd. Generer fra OpenAPI, og
   la byggefeil fortelle deg at API-et har endret seg.
4. **Én kilde til sannhet for styling.** To systemer i samme kodebase blir aldri til ett.
5. **Komponenter er enten dumme og gjenbrukbare, eller smarte og spesifikke.** Ikke
   begge deler i samme fil.
6. **Nettleseren er ikke en betrodd klient.** Legg en BFF imellom — i et metarammeverk er
   den allerede der. Tokenet hører hjemme på serveren, og nettleseren får en `HttpOnly`,
   `Secure`, `SameSite`-cookie. `localStorage.getItem("token")` er lesbart for all JS på
   origin: én XSS eller én kompromittert pakke, og tokenet er ute.
7. **Mål i CI.** Lighthouse / Core Web Vitals som pipeline-steg, på samme måte som
   testene og SonarQube fra samling 1.
8. **Lås avhengigheter, oppdater ofte.** Lockfil i repoet, Dependabot på, og les
   changeloggen for de pakkene som faktisk kjører i produksjon.

---

## 4. Testestrategi

| Nivå | Verktøy | Kjører |
| --- | --- | --- |
| Enhet / komponent | Vitest + Testing Library | Hver PR |
| Integrasjon | MSW som nettverksmock | Hver PR |
| E2E | Playwright, 5–10 kritiske flyter | Nattlig / før deploy |

Test oppførsel, ikke implementasjon: «brukeren ser bestillingen sin i køen», ikke «state
ble satt til PENDING».

---

## 5. Universell utforming

- **WCAG 2.1 AA er forskriftsfestet i Norge** for nettløsninger rettet mot allmennheten,
  i både offentlig og privat sektor.
- EUs tilgjengelighetsdirektiv (EAA) trådte i kraft i EU 28. juni 2025, men er per nå
  **ikke** innlemmet i norsk rett. Det er på vei — sjekk uutilsynet.no for status.
- Verktøy som fanger det meste billig: `eslint-plugin-jsx-a11y`, axe-utvidelsen i
  nettleseren, Lighthouse i CI.
- Det de ikke fanger: tastaturnavigasjon, fokusrekkefølge og skjermleser. Prøv å bruke
  siden din uten mus i to minutter.

Universell utforming er **usynlig i en diff**. Det er derfor det må inn i sjekklista, og
ikke kan overlates til review alene — særlig ikke i AI-generert kode.

---

## 6. Når du reviewer en AI-generert frontend-PR

- **Universell utforming** er usynlig i diffen. Se etter `div` med `onClick`.
- **Plassering av state.** AI defaulter ofte til klient-state for data som hører hjemme
  på serveren.
- **Auth-kode som lander i `localStorage`.** Det dominerer treningsdataene. Se regel 6.
- **Avhengigheter du ikke ba om.** Hver nye pakke er en ny leverandør i
  forsyningskjeden din.

---

## 7. Videre lesning

- [State of JS 2025](https://2025.stateofjs.com) — hva økosystemet faktisk bruker
- [TanStack Query](https://tanstack.com/query) — start med «Important Defaults»
- [Next.js-dokumentasjonen](https://nextjs.org/docs) — særlig caching-kapittelet
- [web.dev](https://web.dev) — Core Web Vitals og ytelse
- `4-Frontend/kaffebar-web` i dette repoet — stacken over, i en app som kjører
