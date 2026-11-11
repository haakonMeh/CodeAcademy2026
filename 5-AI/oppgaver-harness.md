# 🛠️ Workshop 1: Bygg første versjon av harnessen din

**Tid:** 60 min, med en pause på 5–10 min

Du jobber i **ditt eget repo** fra tidligere samlinger. Workshopen har to spor:

- 🟢 **Grunnspor (oppgave 1–4):** For deg som er ny med AI-assistenter, eller ikke har laget instruksjoner og skills før.
- 🔵 **Ekspertspor (oppgave 5–7):** For deg som har tatt AI-Akademiet eller er komfortabel med Copilot. Gjør gjerne oppgave 1–3 raskt først.

Du trenger ikke rekke alt. Det viktigste er at du går hjem med en harness som fungerer.

---

**Tips for hele workshopen:**

- Oppgavene bruker **Copilot CLI**. Du starter den med `copilot` i terminalen, inne i repoet. Filene du lager virker også i VS Code.
- Står du fast, spør AI-en først. Hvis den ikke klarer å hjelpe deg, spør kursholder eller sidemannen.
- **Commit ofte.** Lag en egen branch før du starter, så kan du alltid gå tilbake:

```bash
git checkout -b feature/ai-harness
```

---

## 🟢 Oppgave 1: Mål utgangspunktet (10 min)

**Mål:** Se hvordan AI-en jobber i repoet ditt *uten* harness, så du har noe å sammenligne med.

**Steg:**
1. Finn en **liten, konkret oppgave** i repoet ditt. Eksempler:
   - et nytt endepunkt
   - en ny valideringsregel
   - en ny komponent
   - en enhetstest for en eksisterende klasse
2. Start Copilot CLI i repoet:
   ```bash
   copilot
   ```
3. Gi oppgaven, og skriv ned prompten **ordrett**. Du skal bruke den samme prompten igjen i oppgave 3.
   > *«Lag et nytt endepunkt som returnerer alle aktive brukere. Skriv tester.»*
4. Se på resultatet og noter:
   - Følger koden mønstrene i repoet, som mappestruktur, navngiving og feilhåndtering?
   - Brukte den rammeverkene og hjelpeklassene dere allerede har?
   - Kjørte den testene?
5. Lagre resultatet på en egen branch og gå tilbake:
   ```bash
   git checkout -b eksperiment/uten-harness && git add -A && git commit -m "uten harness"
   git checkout feature/ai-harness
   ```

**Refleksjon:** Hva måtte du rettet før denne koden kunne gått til review?

---

## 🟢 Oppgave 2: Lag instruksjonsfilen (15 min)

**Mål:** Lag en kort instruksjonsfil som lærer AI-en hvordan *ditt* repo fungerer.

**Steg:**
1. Kjør `/init` i Copilot CLI. Den analyserer repoet og lager et utkast til en instruksjonsfil.
2. **Les utkastet kritisk.** Stryk alt som:
   - modellen ser selv i koden
   - er generelle råd, som «skriv ren kode»
   - er feil eller utdatert
3. Legg til det modellen *ikke* kan vite, med utgangspunkt i notatene fra oppgave 1:
   - eksakte kommandoer for bygg, test og lint
   - konvensjoner som gikk galt i oppgave 1
   - hjelpeklasser eller biblioteker den skal bruke
   - ting som er forbudt
4. Be gjerne AI-en om hjelp til å stramme inn teksten:
   > *«Se gjennom instruksjonsfilen. Fjern alt som er åpenbart fra koden eller generelt, og gjør resten så kort og presist som mulig. Ikke legg til nye regler uten å spørre meg.»*
5. Sjekk at filen er **under ca. 60 linjer**.

**Tips:** Skriv regler som konkrete instruksjoner. «Bruk `Result<T>` for feil, ikke exceptions» er bedre enn «Vi liker funksjonell feilhåndtering».

---

## 🟢 Oppgave 3: Test med og uten harness (5 min)

**Mål:** Se om instruksjonsfilen faktisk gjør en forskjell.

**Steg:**
1. Avslutt Copilot og start en **ny økt**, så den ikke husker noe fra oppgave 1.
2. Gi nøyaktig samme prompt som i oppgave 1.
3. Sammenlign med resultatet fra oppgave 1:
   ```bash
   git diff eksperiment/uten-harness -- src/
   ```
   Bytt ut `src/` med mappen der koden din ligger.
4. Er noe fortsatt feil? Oppdater instruksjonsfilen og prøv igjen.

**Refleksjon:** Hvilke regler hadde størst effekt? Var det noen regler AI-en ignorerte, og hvorfor tror du det skjedde?

---

☕ **Pause (5–10 min)**

---

## 🟢 Oppgave 4: Lag din første skill (15 min)

**Mål:** Pakk en oppgave du gjør ofte inn i en skill som teamet kan gjenbruke.

**Steg:**
1. Velg en oppgavetype som går igjen i prosjektet. Eksempler:
   - nytt endepunkt
   - ny komponent
   - ny databasetabell med migrering
   - skrive tester for en klasse
2. Be Copilot lage skillen:
   > *«Lag en ny skill i `.github/skills/nytt-endepunkt/SKILL.md`. Den skal beskrive hvordan vi lager nye REST-endepunkter i dette repoet. Se på eksisterende endepunkter for å finne mønsteret. Skillen skal ha `name` og en `description` som forklarer både hva den gjør og når den skal brukes. Lag stegene som en nummerert liste, og avslutt med å kjøre testene.»*
3. Les gjennom skillen og rett det som er feil.
4. Sjekk at Copilot finner den:
   ```
   /skills list
   ```
   Hvis den ikke dukker opp, kjør `/skills reload`.
5. Test skillen. Be om en ny oppgave av samme type **uten** å nevne skillen, og se om agenten velger den selv:
   > *«Lag et endepunkt for å slette en bruker.»*
6. Commit harnessen:
   ```bash
   git add -A && git commit -m "feat: første versjon av AI harness"
   ```

**Refleksjon:** Brukte agenten skillen uten at du ba om det? Hvis ikke, prøv å gjøre `description` tydeligere.

---

## 🔵 Oppgave 5: Stibaserte instruksjoner

**Mål:** Gi ulike regler til ulike deler av koden, uten å fylle opp konteksten med alt hele tiden.

**Steg:**
1. Finn to områder i repoet som har ulike regler, for eksempel tester og produksjonskode, eller frontend og backend.
2. Lag en fil per område:
   ```
   .github/instructions/tester.instructions.md
   .github/instructions/frontend.instructions.md
   ```
   ```markdown
   ---
   applyTo: 'src/test/**'
   ---
   - ...
   ```
3. Flytt regler fra den globale instruksjonsfilen over i de stibaserte filene der det passer.
4. Gi agenten en oppgave i hvert område, og sjekk at den følger riktige regler.

**Refleksjon:** Hvor mye kortere ble den globale instruksjonsfilen?

---

## 🔵 Oppgave 6: Lag en workflow-skill

**Mål:** Lag en skill som får agenten til å følge teamets arbeidsflyt fra start til slutt, med sjekkpunkter.

**Steg:**
1. Lag `.github/skills/feature-workflow/SKILL.md` med stegene:
   - **forstå:** les kode, og spør hvis kravet er uklart
   - **planlegg:** lag en plan og **vent på godkjenning**
   - **test først:** skriv tester som feiler
   - **implementer:** gjør testene grønne
   - **verifiser:** kjør tester, lint og bygg
   - **oppsummer:** skriv hva som er endret og hva reviewer bør se på
2. Vis til skillen fra oppgave 4 i implementasjonssteget, for eksempel: *«For nye endepunkter: bruk skillen `nytt-endepunkt`.»*
3. Legg en sjekkliste i en egen fil i skill-mappen, for eksempel `sjekkliste.md` med punkter for sikkerhet, logging og feilhåndtering. La `SKILL.md` vise til den, så den bare lastes inn når den trengs.
4. Test workflowen på en ekte, liten oppgave fra backloggen.

**Refleksjon:** Stoppet agenten og ventet på godkjenning av planen? Hvordan kan du formulere sjekkpunktene så de blir fulgt?

---

## 🔵 Oppgave 7: Stresstest harnessen

**Mål:** Finn svakhetene i harnessen før kollegene dine gjør det.

**Steg:**
1. Be agenten vurdere harnessen:
   > *«Les instruksjonsfilene og skillene i dette repoet. Finn regler som er uklare, motstridende, utdaterte eller som ikke stemmer med koden. Foreslå forbedringer, men ikke endre noe.»*
2. Gi agenten en oppgave som er *litt utenfor* det skillene dekker. Hva skjer?
3. Oppdater harnessen, og skriv en kort `README`-seksjon som forklarer hva harnessen består av, så resten av teamet forstår den.

---

## 🏁 Før du går videre

Sjekk at du har:
- ✅ en instruksjonsfil som er kort og presis
- ✅ minst én skill som agenten finner og bruker
- ✅ committet alt på branchen `feature/ai-harness`

I neste modul bygger du videre med **subagenter, orkestrering og guardrails**.
