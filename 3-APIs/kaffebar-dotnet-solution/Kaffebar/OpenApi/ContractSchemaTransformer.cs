using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kaffebar.OpenApi;

/// <summary>
/// Retter opp tre steder der den utledede spesifikasjonen ikke beskriver det API-et
/// faktisk gjør.
/// </summary>
/// <remarks>
/// Dette er den mest lærerike filen i .NET-prosjektet, og den finnes ikke i Java-sporet
/// i det hele tatt. Der ER YAML-en kontrakten. Her er spesifikasjonen en utledning fra
/// koden, og en utledning er bare så presis som utlederen. Tre konkrete eksempler:
///
///   1. <b>DateTimeOffset med egen konverter.</b> Når en type har en
///      <c>JsonConverter</c> vi har skrevet selv, kan skjemageneratoren ikke vite hva
///      den skriver. Resultatet er et tomt skjema — og
///      <c>npx openapi-typescript</c> gir deg <c>createdAt: unknown</c>.
///
///   2. <b>null i enum-listene.</b> Brukes en enum ett sted som <c>MilkType?</c>,
///      legger generatoren <c>null</c> inn i selve enum-skjemaet, som deles av ALLE
///      brukssteder. Da har <c>MilkType</c> plutselig fire verdier og en null.
///
///   3. <b>Valgfritt blir «kan være null».</b> C# skiller ikke mellom «feltet mangler»
///      og «feltet er null», så et <c>MilkType?</c> blir <c>oneOf: [null, MilkType]</c>.
///      Men serveren utelater feltet når det er null (<c>WhenWritingNull</c>), så den
///      sender aldri en eksplisitt null. Kontrakten sier <c>milkType?: MilkType</c>, og
///      det er det som er sant.
///
/// Ingen av de tre endrer ett eneste byte på tråden. De endrer hva spesifikasjonen
/// PÅSTÅR om tråden — og det er den påstanden andre genererer klienter fra.
/// </remarks>
public sealed class ContractSchemaTransformer : IOpenApiSchemaTransformer, IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        // For et felt av typen `MilkType?` er JsonTypeInfo.Type = Nullable<MilkType>.
        // Vi vil resonnere om den underliggende typen.
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;

        // 1. Tidsstempler: string/date-time, slik kontrakten sier.
        if (type == typeof(DateTimeOffset) || type == typeof(DateTime))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "date-time";
        }

        // 2. Ingen null blant enum-verdiene.
        if (type.IsEnum && schema.Enum is { Count: > 0 })
        {
            var withoutNull = schema.Enum.Where(value => value is not null).ToList();
            if (withoutNull.Count != schema.Enum.Count)
            {
                schema.Enum = withoutNull;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Punkt 3 kjører på det ferdige dokumentet og ikke per skjema, fordi
    /// <c>$ref</c>-ene først er på plass når alle skjemaene er bygget. Prøver du å
    /// erstatte en <c>oneOf: [null, $ref]</c> underveis i en skjema-transformer, blir
    /// endringen overskrevet av ref-oppslaget etterpå.
    /// </summary>
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var schema in document.Components?.Schemas?.Values ?? [])
        {
            if (schema is not OpenApiSchema concrete || concrete.Properties is not { Count: > 0 })
            {
                continue;
            }

            foreach (var name in concrete.Properties.Keys.ToList())
            {
                concrete.Properties[name] = Denullify(concrete.Properties[name]);
            }
        }

        return Task.CompletedTask;
    }

    private static IOpenApiSchema Denullify(IOpenApiSchema property)
    {
        if (property is not OpenApiSchema concrete)
        {
            return property;
        }

        // { "type": ["null", "boolean"] } -> { "type": "boolean" }
        if (concrete.Type is { } jsonType && jsonType.HasFlag(JsonSchemaType.Null) && jsonType != JsonSchemaType.Null)
        {
            concrete.Type = jsonType & ~JsonSchemaType.Null;
        }

        // { "oneOf": [ { "type": "null" }, { "$ref": … } ] } -> { "$ref": … }
        // En $ref er sin egen type i Microsoft.OpenApi 2.0, så vi bytter ut hele
        // egenskapen i stedet for å endre den på plass.
        var union = concrete.OneOf ?? concrete.AnyOf;
        if (union is not { Count: 2 })
        {
            return concrete;
        }

        if (union.Count(IsNullSchema) != 1)
        {
            return concrete;
        }

        return union.First(member => !IsNullSchema(member));
    }

    private static bool IsNullSchema(IOpenApiSchema member) =>
        member.Type == JsonSchemaType.Null;
}
