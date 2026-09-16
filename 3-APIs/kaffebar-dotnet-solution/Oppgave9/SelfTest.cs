using System.Text.Json;

namespace Oppgave9;

/// <summary>
/// Beviser at oppsettet virker begge veier. Det er lett å se på attributtene og tro at
/// de gjør jobben; denne kjøringen er forskjellen på å tro og å vite.
/// </summary>
public static class SelfTest
{
    public static int Run(JsonSerializerOptions options)
    {
        var failures = 0;

        // --- Serialisering ---------------------------------------------------
        var request = new CreateMixedOrderRequest("Ada",
        [
            new CoffeeItem(Guid.Parse("c0ffee01-0000-4000-8000-000000000007"),
                CoffeeSize.Medium, MilkType.Oat),
            new PastryItem(Guid.Parse("ba57ee01-0000-4000-8000-000000000001"),
                IsVegan: true, Warmed: true)
        ]);

        var json = JsonSerializer.Serialize(request, options);
        Console.WriteLine("--- serialisert ---");
        Console.WriteLine(json);

        failures += Check("diskriminatoren skrives for kaffe", json.Contains("\"type\": \"coffee\""));
        failures += Check("diskriminatoren skrives for bakst", json.Contains("\"type\": \"pastry\""));
        failures += Check("enum som streng", json.Contains("\"milkType\": \"OAT\""));
        failures += Check("baksten har isVegan", json.Contains("\"isVegan\": true"));
        failures += Check("kaffen har ikke isVegan", !json.Contains("\"isVegan\": false"));

        // --- Deserialisering -------------------------------------------------
        const string incoming = """
            {
              "customerName": "Ada",
              "items": [
                { "type": "coffee", "coffeeId": "c0ffee01-0000-4000-8000-000000000007",
                  "size": "LARGE", "milkType": "SOY" },
                { "type": "pastry", "pastryId": "ba57ee01-0000-4000-8000-000000000001",
                  "isVegan": false, "warmed": true }
              ]
            }
            """;

        var parsed = JsonSerializer.Deserialize<CreateMixedOrderRequest>(incoming, options)!;

        Console.WriteLine("--- deserialisert ---");
        foreach (var item in parsed.Items)
        {
            Console.WriteLine($"  {item.GetType().Name}: {item}");
        }

        failures += Check("to varelinjer", parsed.Items.Count == 2);
        failures += Check("første ble en CoffeeItem", parsed.Items[0] is CoffeeItem);
        failures += Check("andre ble en PastryItem", parsed.Items[1] is PastryItem);
        failures += Check("kaffens størrelse",
            parsed.Items[0] is CoffeeItem { Size: CoffeeSize.Large, MilkType: Oppgave9.MilkType.Soy });
        failures += Check("bakstens felt",
            parsed.Items[1] is PastryItem { IsVegan: false, Warmed: true });

        // --- Diskriminator som ikke finnes -----------------------------------
        const string unknown = """
            {"customerName":"Ada","items":[{"type":"sandwich","foo":1}]}
            """;

        var threw = false;
        try
        {
            JsonSerializer.Deserialize<CreateMixedOrderRequest>(unknown, options);
        }
        catch (JsonException)
        {
            threw = true;
        }

        failures += Check("ukjent diskriminator avvises", threw);

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "OK — alt stemmer." : $"{failures} sjekk(er) feilet.");
        return failures == 0 ? 0 : 1;
    }

    private static int Check(string what, bool ok)
    {
        Console.WriteLine($"  [{(ok ? "ok " : "FEIL")}] {what}");
        return ok ? 0 : 1;
    }
}
