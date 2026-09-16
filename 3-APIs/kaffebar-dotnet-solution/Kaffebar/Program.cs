using System.Text.Json.Serialization;
using Kaffebar.Auth;
using Kaffebar.Errors;
using Kaffebar.Events;
using Kaffebar.Models;
using Kaffebar.OpenApi;
using Kaffebar.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Konfigurasjon -------------------------------------------------------
var eventOptions = builder.Configuration.GetSection(EventOptions.SectionName).Get<EventOptions>()
                   ?? new EventOptions();
var baristaOptions = builder.Configuration.GetSection(BaristaOptions.SectionName).Get<BaristaOptions>()
                     ?? new BaristaOptions();
builder.Services.AddSingleton(eventOptions);
builder.Services.AddSingleton(baristaOptions);

// --- Oppgave 2: begge stilene i samme prosjekt ---------------------------
// Controllers for ordrene. GET /menu ligger igjen som Minimal API lenger nede —
// det er ikke en glipp, det er oppgave 2. Se FASIT.md.
builder.Services.AddControllers()
    .AddJsonOptions(ConfigureJson);

// Minimal API-ene serialiseres av ConfigureHttpJsonOptions, ikke av MVC sine
// JsonOptions. Glemmer du den ene, kommer enums ut som tall fra halve API-et.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// --- Oppgave 6: RFC 7807 Problem Details overalt -------------------------
builder.Services.AddProblemDetails(options =>
{
    // Gjelder feil som ikke går gjennom kontrollerne våre — 404 fra en rute som ikke
    // matcher, 401 fra autentiseringen, 500 fra en uventet exception.
    options.CustomizeProblemDetails = context =>
    {
        var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
        var (slug, title) = status switch
        {
            StatusCodes.Status400BadRequest => ("validation-error", "Ugyldig forespørsel"),
            StatusCodes.Status401Unauthorized => ("unauthorized", "Ikke autentisert"),
            StatusCodes.Status403Forbidden => ("forbidden", "Ingen tilgang"),
            StatusCodes.Status404NotFound => ("not-found", "Ikke funnet"),
            _ => ("internal-error", "Noe gikk galt")
        };

        context.ProblemDetails.Type = $"https://codeacademy.soprasteria.no/problems/{slug}";
        context.ProblemDetails.Title = title;
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path.Value;
    };
});

// --- Oppgave 4: valideringsfeil i kontraktens form -----------------------
// [ApiController] svarer som standard med ValidationProblemDetails, der `errors` er en
// ORDBOK. Kontrakten i 4-Frontend bruker en LISTE av { field, message }. Vi følger
// kontrakten. Se FASIT.md, oppgave 4 — dette er en av prisene for code-first.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = ProblemFactory.ToValidationErrors(context.ModelState);
        return ProblemFactory.Validation(context.HttpContext, errors).AsResult();
    };
});

// --- Lagring -------------------------------------------------------------
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// --- Valgfritt: hendelser til RabbitMQ + auto-barista. AV som standard. --
builder.Services.AddSingleton<OrderEventPublisher>();
if (baristaOptions.AutoEnabled)
{
    builder.Services.AddHostedService<AutoBarista>();
}

// --- Oppgave 10: Entra ID. AV som standard. -----------------------------
builder.Services.AddKaffebarAuthentication(builder.Configuration);

// --- Code-first OpenAPI --------------------------------------------------
// Spesifikasjonen genereres av rammeverket ut fra endepunktene og typene i prosjektet.
builder.Services.AddOpenApi(options =>
{
    // Kontrakten i 4-Frontend kaller feilobjektet `Problem`, .NET kaller sin klasse
    // `ProblemDetails`. Wire-formatet er identisk, men skjemanavnet havner i de
    // genererte typene: src/types/domain.ts i kaffebar-web slår opp
    // components["schemas"]["Problem"]. Så vi gir typen navnet kontrakten bruker.
    options.CreateSchemaReferenceId = typeInfo =>
        typeInfo.Type == typeof(ProblemDetails)
            ? "Problem"
            : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

    options.AddDocumentTransformer<SecuritySchemeTransformer>();
    options.AddDocumentTransformer<ProblemDetailsTransformer>();
    options.AddSchemaTransformer<ContractSchemaTransformer>();
    // Samme klasse, to roller — punkt 3 må kjøre på det ferdige dokumentet.
    options.AddDocumentTransformer<ContractSchemaTransformer>();
    options.AddDocumentTransformer((document, _, _) =>
    {
        // Uten dette heter API-et «Kaffebar | v1» i Scalar.
        document.Info.Title = "Kaffebar API";
        document.Info.Version = "1.0.0";
        document.Info.Description =
            "Code-first API for å administrere bestillinger i en kaffebar. " +
            "Fasit for samling 3, .NET-sporet. Lagrer alt i minne.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Gjør at 404 fra en rute som ikke matcher også får en ProblemDetails-body.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Eksponerer den genererte spesifikasjonen på /openapi/v1.json
    app.MapOpenApi();

    // Scalar gir et moderne, interaktivt UI på /scalar/v1
    app.MapScalarApiReference();

    // Samling 4: `yarn generate:types` i kaffebar-web peker på /openapi.json, fordi
    // alle tre implementasjonene skal kunne brukes med nøyaktig samme kommando.
    // .NET legger den på /openapi/v1.json, Java-sporet på /v3/api-docs. Begge løser
    // det med en omdirigering hit.
    //
    // ExcludeFromDescription() holder ruta ute av det genererte dokumentet — ellers
    // ville aliaset dukket opp som et endepunkt i kontrakten, og deretter i de
    // genererte TypeScript-typene.
    app.MapGet("/openapi.json", () => Results.Redirect("/openapi/v1.json"))
        .ExcludeFromDescription()
        .AllowAnonymous();
}

app.UseKaffebarAuthentication();

app.MapControllers();

// --- Minimal API: menyen -------------------------------------------------
// Oppgave 2 ber eksplisitt om at GET /menu blir liggende som Minimal API mens ordrene
// flyttes til en controller. Det ser inkonsekvent ut, og det er poenget: nå kan du se
// de to stilene ved siden av hverandre. IKKE «rydd» dette til én stil.
app.MapGet("/menu", (IOrderRepository repository) => repository.Menu)
    .WithName("GetMenu")
    .WithSummary("Hent kaffemeny")
    .WithDescription("Returnerer en liste over alle tilgjengelige kaffedrikker i kaffebaren.")
    .Produces<IReadOnlyList<Coffee>>()
    .AllowAnonymous();

// --- Minimal API: helsesjekk --------------------------------------------
// Ikke en del av oppgavesettet, men en del av referansekontrakten — Docker bruker den.
var startedAt = DateTimeOffset.UtcNow;
app.MapGet("/health", (OrderEventPublisher events) => new Health(
        HealthStatus.Ok,
        events.State,
        Math.Round((DateTimeOffset.UtcNow - startedAt).TotalSeconds, 3)))
    .WithName("GetHealth")
    .WithSummary("Helsesjekk")
    .Produces<Health>()
    .AllowAnonymous();

app.Run();

static void ConfigureJson(Microsoft.AspNetCore.Mvc.JsonOptions options)
{
    // Enums som STRENGER, ikke tall. Uten denne blir "PENDING" til 0, og kontrakten er
    // brutt. Dette er den vanligste enkeltfeilen i .NET-sporet.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    // Valgfrie felt som ikke er satt skal FORSVINNE fra JSON-en, ikke komme ut som
    // null. Kontrakten sier `milkType?: MilkType` — altså fraværende, ikke null.
    // Referanse-implementasjonen gjør det samme.
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    // Tidsstempler som 2026-09-13T09:41:12.004Z, slik de tre implementasjonene skal
    // skrive dem likt. Se UtcDateTimeOffsetConverter.
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());

    // JsonSerializerDefaults.Web tillater som standard at tall leses fra strenger
    // ("quantity": "2"). Det gjør at .NET beskriver quantity som `integer | string` i
    // den genererte spesifikasjonen, og da stemmer den ikke med kontrakten lenger.
    // Strict gir både strengere validering og et riktigere skjema.
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
}

// WebApplicationFactory<Program> i testprosjektet trenger å se denne typen.
public partial class Program;
