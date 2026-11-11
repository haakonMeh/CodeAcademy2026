# 🛠️ Workshop 2: Bygg et agentteam med guardrails

**Tid:** 60 min, med en pause på 5–10 min. De siste 5–10 minuttene viser vi hverandre hva vi har laget.

Du fortsetter på branchen `feature/ai-harness` i ditt eget repo.

- 🟢 **Grunnspor (oppgave 1–4):** Lag to agenter med riktig modell og riktige tilganger. Koble dem sammen, og legg inn en guardrail.
- 🔵 **Ekspertspor (oppgave 5–7):** Orkestrator, evaluator–optimizer-løkke og parallelle agenter.

---

## 🟢 Oppgave 1: Lag en planner-agent (10 min)

**Mål:** Lag en agent som planlegger, men aldri endrer kode.

**Steg:**
1. Lag filen `.github/agents/planner.agent.md`:
   ```markdown
   ---
   name: planner
   description: Lager implementasjonsplaner for nye features og endringer. Bruk før implementasjon av noe som berører mer enn én fil.
   tools: ['read', 'search']
   model: Claude Opus 5.5
   ---
   Du er en erfaren tech lead. Du endrer ALDRI filer.
   1. Les relevant kode og instruksjonsfilene i repoet.
   2. Still oppklarende spørsmål hvis kravet er uklart.
   3. Lever en plan med: berørte filer, steg i rekkefølge, tester som må skrives, og risiko.
   Hold planen kort, maks én skjermside.
   ```
2. Sjekk hvilke modeller du har tilgang til med `/model`, og juster `model:` hvis det trengs.
3. Test agenten:
   ```bash
   copilot --agent planner -p "Lag en plan for: <en liten feature fra backloggen din>"
   ```
   Du kan også velge agenten med `/agent` inne i en Copilot-økt.

**Refleksjon:** Hvorfor får planneren bare `read` og `search`? Hvorfor er en kraftig modell riktig her?

---

## 🟢 Oppgave 2: Lag en read-only reviewer (10 min)

**Mål:** Lag en reviewer som gir et uavhengig blikk på koden.

**Steg:**
1. Lag `.github/agents/reviewer.agent.md`. Bruk eksempelet fra teorien som utgangspunkt.
2. **Velg modell bevisst.** Bruk gjerne en *annen* modellfamilie enn den du implementerer med. Skriver du med Claude, kan du for eksempel reviewe med GPT, eller omvendt.
3. Legg til teamets egne review-kriterier i agenten, med utgangspunkt i instruksjonsfilen fra modul 1.
4. Test agenten på en endring du har gjort:
   > *«Bruk reviewer-agenten til å gå gjennom endringene på denne branchen mot main.»*

**Refleksjon:** Fant reviewer noe du selv hadde oversett? Var noen av funnene støy, og hvordan kan du stramme inn instruksjonene?

---

☕ **Pause (5–10 min)**

---

## 🟢 Oppgave 3: Koble agentene sammen (15 min)

**Mål:** Kjør en hel kjede: planlegg → godkjenn → implementer → review.

**Steg:**
1. Start en ny økt med hovedagenten:
   ```bash
   copilot
   ```
2. Gi en oppgave som bruker begge agentene:
   > *«Jeg vil implementere <feature>. Bruk planner-agenten til å lage en plan og vis meg den. Vent på godkjenning. Implementer deretter planen og kjør testene. Til slutt skal reviewer-agenten gå gjennom endringene. Fiks alle 🔴-funn.»*
3. Følg med på hva som skjer:
   - Ble subagentene brukt?
   - Fikk du et sjekkpunkt før implementasjonen startet?
4. Oppdater gjerne `feature-workflow`-skillen fra modul 1, så den bruker agentene automatisk.

**Refleksjon:** Hvilket orchestration pattern var dette? Hva ville skjedd om alt hadde foregått i én agent?

---

## 🟢 Oppgave 4: Legg inn en guardrail med hook (15 min)

**Mål:** Stopp farlige kommandoer *før* de kjøres, uansett hva agenten blir bedt om.

**Steg:**
1. Lag skriptet `.github/hooks/guard.sh`:
   ```bash
   #!/usr/bin/env bash
   # Leser verktøykallet fra stdin og blokkerer farlige mønstre
   input=$(cat)

   deny() {
     printf '{"permissionDecision":"deny","permissionDecisionReason":"%s"}\n' "$1"
     exit 0
   }

   echo "$input" | grep -Eq 'rm -(rf|fr)'               && deny "rm -rf er blokkert av teamets guardrails"
   echo "$input" | grep -Eq 'git push.*(--force| -f)'   && deny "Force push er ikke tillatt"
   echo "$input" | grep -Eq '\.env'                     && deny "Agenten får ikke lese .env-filer"
   exit 0
   ```
2. Gjør skriptet kjørbart:
   ```bash
   chmod +x .github/hooks/guard.sh
   ```
3. Lag konfigurasjonen `.github/hooks/guardrails.json`:
   ```json
   {
     "version": 1,
     "hooks": {
       "preToolUse": [
         { "type": "command", "matcher": "bash", "bash": "./.github/hooks/guard.sh", "timeoutSec": 10 }
       ]
     }
   }
   ```
4. Start en ny Copilot-økt og test om guardrailen virker:
   > *«Vis innholdet i .env-filen.»*
   > *«Kjør `git push --force`.»*
5. Se at kallene blir blokkert, og les begrunnelsen agenten får.

> 🪟 **Windows:** Bruk Git Bash eller WSL, eller be Copilot skrive en PowerShell-versjon av skriptet. Sjekk hooks-dokumentasjonen for riktig felt i konfigurasjonen.

> ⚠️ Dette er et *læringseksempel*. Et mønster i et skript er lett å omgå, for eksempel med `cat .e""nv`. Bruk hooks sammen med deny-lister, sandbox og review. Hooks er ikke den eneste beskyttelsen. Få guardrails sikkerhetsgjennomgått før de tas i bruk i et kundeprosjekt.

**Bonus:** Prøv i tillegg deny-lister i CLI-en:
```bash
copilot --deny-tool='shell(git push)' --deny-tool='shell(rm)'
```

---

## 🔵 Oppgave 5: Orkestrator med modellvalg

**Mål:** Lag en orkestrator som fordeler arbeid på spesialiserte agenter, med riktig modell til hver av dem.

**Steg:**
1. Lag disse agentene, i tillegg til `planner` og `reviewer`:
   - `utforsker`: lett modell, `read`/`search`. Finner relevant kode og returnerer et kort sammendrag.
   - `implementer`: allsidig modell, `read`/`search`/`edit`/`execute`. Skriver kode og tester.
2. Lag `.github/agents/orkestrator.agent.md`:
   - kraftig modell
   - `tools: ['read', 'search', 'agent']`, så den **ikke** kan redigere selv
   - instruks om å delegere til riktige agenter, og hva hver av dem skal returnere
3. Lag en tabell i README-en som begrunner modellvalget for hver agent.
4. Kjør orkestratoren på en feature som berører flere filer.

**Refleksjon:** Hvor mye kontekst brukte orkestratoren sammenlignet med en enkelt agent? Ble resultatet bedre, eller bare dyrere?

---

## 🔵 Oppgave 6: Evaluator–optimizer-løkke

**Mål:** La implementer og reviewer jobbe i løkke til kvaliteten er god nok, med en trygg stoppbetingelse.

**Steg:**
1. Lag en skill `.github/skills/kvalitetsloop/SKILL.md` som beskriver løkken:
   - implementer lager endringen
   - reviewer vurderer etter faste kriterier
   - finnes det 🔴-funn, fikser implementer dem og reviewer vurderer på nytt
   - **stopp** når det ikke er flere 🔴-funn, eller etter **maks 3 runder**
   - hvis taket nås, skal agenten stoppe, oppsummere funnene som gjenstår og spørre et menneske
2. Test løkken på en oppgave der du *vet* at første forsøk ofte har feil.

**Refleksjon:** Hva skjer uten maks antall runder? Hvordan vet du at reviewer-kriteriene er gode nok?

---

## 🔵 Oppgave 7: Parallelle agenter med `/fleet`

**Mål:** Kjør uavhengige deloppgaver samtidig, uten at agentene går i beina på hverandre.

**Steg:**
1. Finn 2–3 *uavhengige* oppgaver som berører ulike filer, for eksempel tester for tre ulike klasser.
2. I Copilot CLI: be om en plan med `/plan`, og kjør den deretter parallelt:
   ```
   /fleet Skriv enhetstester for KlasseA, KlasseB og KlasseC. Hver deloppgave skal bare endre sin egen testfil.
   ```
3. Følg med på subagentene med `/tasks`.
4. Hvilken modell brukte subagentene? Prøv å angi en modell eller en custom agent med `@agentnavn` i prompten.

**Refleksjon:** Hvilke oppgaver egner seg *ikke* for parallell kjøring?

---

## 🏁 Avslutning (5–10 min)

1. Commit harnessen og lag en merge request:
   ```bash
   git add -A && git commit -m "feat: agentteam og guardrails i AI harness"
   ```
2. **Del med gruppen:**
   - Hvilken agent eller skill var mest nyttig?
   - Hvilket modellvalg overrasket deg?
   - Hva tar du med inn i prosjektet ditt i morgen?

**Ta med videre:** Harnessen er aldri ferdig. Hver gang AI-en gjør en feil, forbedrer du instruksjonene, skillene eller agentene.
