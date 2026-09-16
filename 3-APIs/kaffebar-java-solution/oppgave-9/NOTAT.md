# Oppgave 9 — polymorfisme, og hva generatoren faktisk gjorde

> **Kjør det selv:**
> ```bash
> mvn -f oppgave-9/pom.xml clean test
> ```
> Det genererer koden fra `kaffebar-bakst.yaml`, kompilerer den og kjører de to
> testene som beviser at både serialisering og deserialisering virker.

## Hvorfor dette ligger i en egen mappe

Oppgave 9 er merket ekspertnivå, og den er løst — men **ikke i hovedkontrakten**.

Legger du `oneOf` inn i `Order`, endrer du formen på ordre-objektet. Da brekker
wire-kompatibiliteten med `4-Frontend/kaffebar-api`, og den genererte
TypeScript-klienten i samling 4 må skrives om. Referanse-implementasjonen har utelatt
oppgave 9 av nøyaktig samme grunn — se `4-Frontend/kaffebar-api/README.md`.

Så: egen YAML, eget miniprosjekt, samme generator og samme `configOptions` som
hovedprosjektet. Da er det generatorens oppførsel du ser, ikke en annen konfigurasjon.

## Hva som ble generert

`OrderItem` ble **et interface med to implementasjoner** — ikke en abstrakt baseklasse,
og ikke en wrapper-klasse med to nullable felt:

```java
@JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.PROPERTY,
              property = "type", visible = true)
@JsonSubTypes({
  @JsonSubTypes.Type(value = CoffeeItem.class, name = "coffee"),
  @JsonSubTypes.Type(value = PastryItem.class, name = "pastry"),
  @JsonSubTypes.Type(value = CoffeeItem.class, name = "CoffeeItem"),
  @JsonSubTypes.Type(value = PastryItem.class, name = "PastryItem")
})
public interface OrderItem {
    public String getType();
}
```

```java
public class CoffeeItem implements OrderItem { ... }
public class PastryItem implements OrderItem { ... }
```

Og i `MixedOrder`:

```java
private List<OrderItem> items = new ArrayList<>();
```

Tre ting er verdt å merke seg:

1. **Interface, ikke arv.** Det er et bevisst valg fra generatoren: `oneOf` i JSON
   Schema betyr «nøyaktig én av disse», ikke «arver fra». `CoffeeItem` og `PastryItem`
   deler ingen felt, så det er ingenting å arve. De deler bare *rollen* som ordrelinje,
   og det er akkurat det et interface uttrykker.
2. **`discriminator` gjør hele jobben.** Den blir til `@JsonTypeInfo` +
   `@JsonSubTypes`. Uten `discriminator` i YAML-en får du fortsatt et interface, men
   Jackson har da ingen måte å vite hvilken klasse et innkommende objekt er, og
   deserialiseringen feiler. Skriver du `oneOf` uten discriminator, har du beskrevet
   noe du ikke kan lese inn igjen.
3. **Mappingen står to ganger.** Generatoren legger inn både `mapping`-verdiene
   (`"coffee"`, `"pastry"`) og skjemanavnene (`"CoffeeItem"`, `"PastryItem"`) som
   gyldige type-navn. Det er defensivt, og betyr at API-et ditt aksepterer
   `{"type": "CoffeeItem"}` også, selv om kontrakten din bare lover `"coffee"`. Verdt å
   vite hvis du er streng på hva du tar imot.

## Hva som måtte fikles med

Én ting, og den stopper byggingen helt.

Den naturlige måten å skrive diskriminator-feltet på er å låse det til én verdi:

```yaml
CoffeeItem:
  properties:
    type:
      type: string
      enum: [coffee]     # <- dette ser riktig ut
```

Det kompilerer ikke:

```
CoffeeItem is not abstract and does not override abstract method getType() in OrderItem
getType() in CoffeeItem cannot implement getType() in OrderItem
  return type CoffeeItem.TypeEnum is not compatible with java.lang.String
```

Generatoren lager `String getType()` i interfacet (fordi diskriminatoren i
`components/schemas/OrderItem` er en streng), men en indre `enum TypeEnum` i hver
implementasjon (fordi feltet der har `enum`). De to er uforenlige, og generatoren
oppdager det ikke.

**Løsningen:** la diskriminator-feltet stå som en ren `string` i undertypene. De
lovlige verdiene ligger uansett i `discriminator.mapping`, så kontrakten er like
presis — den er bare presis ett sted i stedet for to.

Det er dette oppgaveteksten mener med *«krever ofte litt fikling med generatoren»*, og
det er et generelt mønster i contract-first: generatoren er ikke en kompilator. Den
oversetter skjema til kode så godt den kan, og der to oversettelser ikke passer sammen,
finner du det ut ved å bygge — ikke ved å lese YAML-en.

## Hva testene beviser

`src/test/java/.../PolymorfismeTest.java`, kjørt mot Jackson 3 (det Spring Boot 4
bruker):

| Test | Hva den viser |
| --- | --- |
| `serializesDiscriminator` | Jackson skriver `"type": "coffee"` / `"type": "pastry"` selv. Du setter den ikke i koden — `@JsonIgnoreProperties(value = "type", allowSetters = true)` på interfacet sørger for at en manuelt satt verdi blir ignorert ved skriving. |
| `deserializesIntoCorrectSubtype` | `{"type": "pastry", ...}` kommer tilbake som en `PastryItem`, med `isVegan` og `warmed` intakt. Diskriminatoren velger klassen. |

## Det som overrasket

At den delen som *ser* vanskeligst ut — Jackson-oppsettet for polymorf
(de)serialisering — kom helt gratis, mens det som ser trivielt ut — å låse
diskriminatoren til én verdi — var det som brakk byggingen.

Og at generatoren ikke advarer. Den skriver ut Java-kode som ikke kompilerer, uten et
eneste varsel. Contract-first gir deg mye, men den gir deg ikke et sikkerhetsnett
mellom YAML-en og koden. Bygget er sikkerhetsnettet.
