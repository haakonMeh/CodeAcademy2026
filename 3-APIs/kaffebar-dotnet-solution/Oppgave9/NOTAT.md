# Oppgave 9 — polymorfisme, og hva `/openapi/v1.json` faktisk ble

> **Kjør det selv:**
> ```bash
> dotnet run --project Oppgave9 -- --selftest   # serialisering + deserialisering
> dotnet run --project Oppgave9                 # API på http://localhost:5036
> ```
> `--selftest` skriver ut JSON-en og kjører elleve sjekker. Uten flagget starter et
> lite API med `/orders-v2`, `/openapi/v1.json` og `/scalar/v1`.

## Hvorfor dette ligger i et eget prosjekt

Oppgave 9 er merket ekspertnivå, og den er løst — men **ikke i hovedmodellen**.

Legger du `[JsonPolymorphic]` inn i `Kaffebar/Models/Orders.cs`, endrer du formen på
`Order`. Da brekker wire-kompatibiliteten med `4-Frontend/kaffebar-api`, og den
genererte TypeScript-klienten i samling 4 må skrives om. Referanse-implementasjonen har
utelatt oppgave 9 av nøyaktig samme grunn — se
`4-Frontend/kaffebar-api/README.md`.

## Modelleringen

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CoffeeItem), "coffee")]
[JsonDerivedType(typeof(PastryItem), "pastry")]
public abstract record OrderItem;

public sealed record CoffeeItem(Guid CoffeeId, CoffeeSize Size, MilkType? MilkType = null,
                                bool? ExtraShot = null) : OrderItem;
public sealed record PastryItem(Guid PastryId, bool IsVegan, bool? Warmed = null) : OrderItem;
```

Merk at `type` **ikke er deklarert som en egenskap** noe sted. System.Text.Json skriver
den selv og leser den selv. Legger du til en egen `Type`-egenskap i tillegg, får du
feltet to ganger i JSON-en.

## Virker det? Ja, begge veier

`--selftest` skriver ut dette:

```json
{
  "customerName": "Ada",
  "items": [
    { "type": "coffee", "coffeeId": "c0ffee01-…-000000000007", "size": "MEDIUM", "milkType": "OAT" },
    { "type": "pastry", "pastryId": "ba57ee01-…-000000000001", "isVegan": true, "warmed": true }
  ]
}
```

og leser det samme tilbake til `CoffeeItem` og `PastryItem`. En ukjent diskriminator
(`"type": "sandwich"`) gir `JsonException` — den blir altså ikke stille ignorert, og i
API-et havner den som en 400.

## Og hva ble `/openapi/v1.json`?

Her er det interessante. Oppgaveteksten spør: *«Får du `oneOf` med en discriminator?»*

**Nei. Du får `anyOf`.**

```json
"OrderItem": {
  "required": ["type"],
  "type": "object",
  "anyOf": [
    { "$ref": "#/components/schemas/OrderItemCoffeeItem" },
    { "$ref": "#/components/schemas/OrderItemPastryItem" }
  ],
  "discriminator": {
    "propertyName": "type",
    "mapping": {
      "coffee": "#/components/schemas/OrderItemCoffeeItem",
      "pastry": "#/components/schemas/OrderItemPastryItem"
    }
  }
}
```

Tre ting å legge merke til:

**1. `anyOf`, ikke `oneOf`.** OpenAPI tillater `discriminator` sammen med `oneOf`,
`anyOf` og `allOf`, så spesifikasjonen er gyldig. Men semantikken er svakere: `oneOf`
betyr «nøyaktig én», `anyOf` betyr «minst én». Java-sporet, der kontrakten er
håndskrevet, sier `oneOf` — fordi den som skrev den mente `oneOf`. Her er det
rammeverket som velger, og det velger den løsere varianten.

**2. Skjemanavnene er ikke klassenavnene.** Undertypene heter `OrderItemCoffeeItem` og
`OrderItemPastryItem` — basetypen plus den avledede. I Java-sporet heter de `CoffeeItem`
og `PastryItem`. Wire-formatet er identisk, men typenavnene i en generert klient blir
ulike. Enda et argument for at kontrakten bør være noe du bestemmer, ikke noe som
faller ut.

**3. `type` er `required` på basetypen, men *valgfri* i undertypene.** Kjør
`npx openapi-typescript http://localhost:5036/openapi/v1.json` og du får:

```ts
OrderItem: components["schemas"]["OrderItemCoffeeItem"] | components["schemas"]["OrderItemPastryItem"];

OrderItemCoffeeItem: {
    /** @enum {string} */
    type?: "coffee";        // <- valgfri
    coffeeId: string;
    size: components["schemas"]["CoffeeSize"];
    ...
};
```

Det er en union, som er riktig. Men fordi `type` er `type?`, er den **ikke en
diskriminert union** i TypeScript: `if (item.type === "coffee")` snevrer ikke typen inn
til `OrderItemCoffeeItem`. Klienten må gjøre et `as`-kast eller skrive en egen type
guard.

## Det som overrasket

At den vanskelige delen var gratis, og den enkle delen ikke var det.

Polymorf serialisering og deserialisering — som er den biten man forventer å bruke tid
på — kom helt uten kode: to attributter, og det virker begge veier, med fornuftig
feilhåndtering på ukjente diskriminatorer.

Men **beskrivelsen** av det samme ble omtrentlig. `anyOf` der man mente `oneOf`,
skjemanavn man ikke valgte, og et diskriminator-felt som er påkrevd på ett nivå og
valgfritt på et annet — nok til at en generert klient ikke kan narrowe på det.

Og det er hele forskjellen på code-first og contract-first i én prøve: code-first gir
deg en spesifikasjon som er *utledet*, og en utledning er bare så presis som
utlederen. Contract-first gir deg en spesifikasjon som er *bestemt*. For et internt API
der begge sider eies av samme team, er utledningen mer enn god nok. For et API andre
skal generere klienter fra, er forskjellen mellom `oneOf` og `anyOf` noe du vil ha
kontroll over.

> **Vil du overstyre det?** Du kan skrive en `IOpenApiSchemaTransformer` som bytter
> `anyOf` til `oneOf` og setter `type` som `required` i undertypene — på samme måte som
> `OpenApi/SecuritySchemeTransformer.cs` og `OpenApi/ProblemDetailsTransformer.cs` i
> hovedprosjektet fyller inn det rammeverket ikke kan utlede. Poenget her var å vise hva
> du får uten å gjøre det.
