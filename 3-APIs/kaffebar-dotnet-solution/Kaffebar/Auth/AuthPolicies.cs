using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace Kaffebar.Auth;

/// <summary>Innstillingene for oppgave 10. Alt er av som standard.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Kaffebar:Auth";

    /// <summary>
    /// AV som standard. Er den PÅ, må alle kall mot status-endepunktene ha et gyldig
    /// token — og da kan ikke frontenden i samling 4 snakke med API-et uten at du
    /// bygger inn innlogging der også. Bonussteget der ville brukket.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <c>EntraId</c> validerer tokens mot Microsoft Entra ID (<c>AzureAd</c>-seksjonen).
    /// <c>LocalJwt</c> bruker den vanlige JWT Bearer-handleren, som leser konfigurasjon
    /// fra <c>Authentication:Schemes:Bearer</c> — altså den <c>dotnet user-jwts</c>
    /// skriver inn i user secrets.
    ///
    /// Uten en ekte tenant er <c>LocalJwt</c> den eneste måten å faktisk VERIFISERE at
    /// 401/200-oppførselen virker. Se FASIT.md, oppgave 10.
    /// </summary>
    public AuthMode Mode { get; set; } = AuthMode.EntraId;

    /// <summary>Krev i tillegg rollen «Barista». Bonusdelen av oppgave 10.</summary>
    public bool RequireBaristaRole { get; set; }
}

public enum AuthMode
{
    EntraId,
    LocalJwt
}

/// <summary>Navnene på autorisasjonspolicyene.</summary>
public static class AuthPolicies
{
    /// <summary>
    /// Brukes på status-endepunktene. Når autentisering er AV, slipper policyen alle
    /// gjennom — se <see cref="AuthenticationSetup"/>. Da trenger vi ingen
    /// <c>#if</c>-er eller to sett med attributter i kontrolleren: ett attributt,
    /// og konfigurasjonen avgjør hva det betyr.
    /// </summary>
    public const string Barista = "Barista";
}

/// <summary>
/// Oppgave 10: JWT-autentisering mot Microsoft Entra ID.
/// </summary>
/// <remarks>
/// Koden og konfigurasjonen er komplett, men <b>slått av</b>
/// (<c>Kaffebar:Auth:Enabled = false</c>). Det er et bevisst valg: skrur du den på,
/// kan ikke frontenden i samling 4 lenger snakke med API-et.
///
/// Uten en ekte tenant kan du likevel verifisere hele flyten lokalt med
/// <c>dotnet user-jwts</c> — se FASIT.md, oppgave 10.
/// </remarks>
public static class AuthenticationSetup
{
    public static IServiceCollection AddKaffebarAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
                      ?? new AuthOptions();
        services.AddSingleton(options);

        if (options.Enabled)
        {
            var authentication = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

            if (options.Mode == AuthMode.EntraId)
            {
                // Den ekte varen. Krever en app-registrering i Entra ID og en utfylt
                // AzureAd-seksjon — se FASIT.md.
                authentication.AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
            }
            else
            {
                // Lokal verifisering uten tenant.
                //
                // `dotnet user-jwts create` legger issuer og audiences i
                // appsettings.Development.json og selve signeringsnøkkelen i user
                // secrets (altså UTENFOR repoet — ingen hemmeligheter sjekkes inn).
                // Vi leser begge deler eksplisitt her. Det er noen linjer mer enn et
                // bart `AddJwtBearer()`, men det er også helt tydelig hva som
                // valideres — og det virker uavhengig av hvordan den automatiske
                // konfigurasjonsbindingen oppfører seg i den SDK-versjonen du har.
                var section = configuration.GetSection("Authentication:Schemes:Bearer");

                authentication.AddJwtBearer(jwt =>
                {
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = section["ValidIssuer"],
                        ValidateAudience = true,
                        ValidAudiences = section.GetSection("ValidAudiences").Get<string[]>(),
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = section.GetSection("SigningKeys").GetChildren()
                            .Select(key => key["Value"])
                            .Where(value => !string.IsNullOrEmpty(value))
                            .Select(value => new SymmetricSecurityKey(Convert.FromBase64String(value!)))
                            .ToArray(),
                        ValidateLifetime = true
                    };
                    // RoleClaimType settes bevisst IKKE. JWT-handleren mapper som
                    // standard «role» (og Entra sin «roles») til ClaimTypes.Role, og
                    // det er det RequireRole("Barista") ser etter. Overstyrer du den
                    // til "role", leter autorisasjonen etter et claim som mappingen
                    // nettopp har døpt om — og du får 403 med et token som er riktig.
                });
            }
        }

        services.AddAuthorization(authorization =>
        {
            authorization.AddPolicy(AuthPolicies.Barista, policy =>
            {
                if (!options.Enabled)
                {
                    // Autentisering er av: policyen er en gjennomkjøring. Endepunktene
                    // beholder [Authorize]-attributtet sitt, men det koster ingenting.
                    policy.RequireAssertion(_ => true);
                    return;
                }

                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();

                if (options.RequireBaristaRole)
                {
                    // Entra ID legger app roles i «roles»-claimet.
                    policy.RequireRole("Barista");
                }
            });
        });

        return services;
    }

    public static WebApplication UseKaffebarAuthentication(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AuthOptions>();

        if (options.Enabled)
        {
            app.UseAuthentication();
        }

        // UseAuthorization må stå uansett — det er den som evaluerer policyen over.
        app.UseAuthorization();
        return app;
    }
}
