using Kaffebar.Auth;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kaffebar.OpenApi;

/// <summary>
/// Oppgave 10, siste kulepunkt: legg <c>securitySchemes</c> i den genererte
/// spesifikasjonen slik at Scalar viser et «Authorize»-felt.
/// </summary>
/// <remarks>
/// Dette er verdt å legge merke til som et generelt poeng om code-first: rammeverket
/// utleder alt det kan fra koden, men det finnes ting det ikke kan utlede. Hvordan man
/// får tak i et token er en av dem. Da skriver du en transformer.
///
/// Transformeren kjører bare når autentisering faktisk er PÅ. Ellers ville
/// spesifikasjonen lovet en sikkerhet som ikke finnes.
/// </remarks>
public sealed class SecuritySchemeTransformer(AuthOptions options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return Task.CompletedTask;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token fra Microsoft Entra ID. Lim inn uten «Bearer »-prefikset."
        };

        return Task.CompletedTask;
    }
}
