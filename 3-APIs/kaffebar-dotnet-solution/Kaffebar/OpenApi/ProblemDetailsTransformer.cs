using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kaffebar.OpenApi;

/// <summary>
/// Dokumenterer <c>errors</c>-lista på <c>Problem</c> (.NET sin <c>ProblemDetails</c>).
/// </summary>
/// <remarks>
/// Et konkret eksempel på grensen for code-first: rammeverket utleder skjemaet for
/// <c>ProblemDetails</c> fra klassen, men <c>errors</c> ligger i
/// <c>ProblemDetails.Extensions</c> — en ordbok. Den kan ikke utledes, for typen sier
/// ingenting om hva som havner der.
///
/// API-et sender feltet uansett (se <c>ProblemFactory.Validation</c>), så hvis vi ikke
/// beskriver det, lyver spesifikasjonen ved utelatelse. Derfor denne transformeren.
///
/// Contract-first har ikke dette problemet: der står <c>errors</c> i YAML-en, og
/// koden må forholde seg til den. Her er det omvendt, og da må du huske det selv.
/// </remarks>
public sealed class ProblemDetailsTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        document.Components.Schemas["ValidationError"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Title = "ValidationError",
            Required = new HashSet<string> { "field", "message" },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["field"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description = "Feltet som feilet, som en peker inn i requesten."
                },
                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String }
            }
        };

        if (document.Components.Schemas.TryGetValue("Problem", out var schema)
            && schema is OpenApiSchema problem)
        {
            // Kontrakten sier at type, title og status alltid er med. Det gjør de også
            // — ProblemFactory setter alle tre — men .NET kan ikke utlede det fra
            // ProblemDetails-klassen, der alle egenskapene er nullable.
            problem.Required = new HashSet<string> { "type", "title", "status" };

            problem.Properties ??= new Dictionary<string, IOpenApiSchema>();
            problem.Properties["errors"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Description = "Feltene som ikke validerte. Kun ved 400.",
                Items = new OpenApiSchemaReference("ValidationError", document)
            };
        }

        return Task.CompletedTask;
    }
}
