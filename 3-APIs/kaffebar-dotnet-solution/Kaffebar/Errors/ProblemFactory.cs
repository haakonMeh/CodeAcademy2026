using System.Text.Json;
using Kaffebar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kaffebar.Errors;

/// <summary>
/// Oppgave 6: alle feilsvar lages ett sted, og ser derfor like ut.
/// </summary>
/// <remarks>
/// .NET har RFC 7807 innebygd via <see cref="ProblemDetails"/>, og det er den vi
/// bruker. To ting er justert slik at svaret matcher kontrakten i
/// 4-Frontend/kaffebar-api:
///
/// 1. <c>type</c> peker på våre egne feiltyper i stedet for RFC-lenkene .NET bruker
///    som standard, og <c>title</c> er på norsk — nøyaktig de samme strengene som
///    TypeScript-fasiten og Java-fasiten sender.
///
/// 2. <c>errors</c> er en LISTE av <c>{ field, message }</c>, ikke .NET sin ordbok
///    <c>{ "CustomerName": ["..."] }</c>. Se FASIT.md, oppgave 4: dette er ett av
///    stedene code-first koster deg noe, fordi rammeverkets standardform ikke er den
///    samme som kontraktens.
/// </remarks>
public static class ProblemFactory
{
    private const string TypeBase = "https://codeacademy.soprasteria.no/problems";

    public static ProblemDetails NotFound(HttpContext context, string detail) =>
        Create(context, StatusCodes.Status404NotFound, "not-found", "Ikke funnet", detail);

    public static ProblemDetails Validation(
        HttpContext context, IReadOnlyList<ValidationError> errors)
    {
        var fields = errors.Select(error => error.Field).Distinct().ToList();
        var problem = Create(context, StatusCodes.Status400BadRequest, "validation-error",
            "Ugyldig forespørsel",
            $"Valideringen feilet for {fields.Count} felt: {string.Join(", ", fields)}.");

        problem.Extensions["errors"] = errors;
        return problem;
    }

    public static ProblemDetails Create(
        HttpContext context, int status, string slug, string title, string? detail)
    {
        return new ProblemDetails
        {
            Type = $"{TypeBase}/{slug}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path.Value
        };
    }

    /// <summary>
    /// Oversetter .NET sin <see cref="ModelStateDictionary"/> til kontraktens
    /// <c>errors</c>-liste.
    /// </summary>
    /// <remarks>
    /// To detaljer som er lette å gå glipp av:
    ///
    /// - Nøklene i ModelState er C#-navnene (<c>CustomerName</c>). Kontrakten bruker
    ///   camelCase (<c>customerName</c>), så vi mapper.
    /// - Feiler deserialiseringen — for eksempel <c>"size": "HUGE"</c> — bruker
    ///   System.Text.Json en JSON-peker som nøkkel (<c>$.size</c>). Vi stripper
    ///   <c>$.</c> slik at feltnavnet ser likt ut uansett hvilken vei feilen kom.
    /// </remarks>
    public static List<ValidationError> ToValidationErrors(ModelStateDictionary modelState)
    {
        var errors = new List<ValidationError>();

        foreach (var (key, entry) in modelState)
        {
            foreach (var error in entry.Errors)
            {
                errors.Add(new ValidationError(FieldName(key), Message(error)));
            }
        }

        // Feiler deserialiseringen, legger MVC på en ekstra feil på selve
        // request-objektet ("The request field is required") fordi bodyen ble null.
        // Den sier ingenting nyttig når vi allerede vet hvilket felt som var galt.
        if (errors.Count > 1)
        {
            errors.RemoveAll(error => error.Field is "request");
        }

        return errors;
    }

    /// <summary>
    /// Rydder opp i meldingen.
    /// </summary>
    /// <remarks>
    /// System.Text.Json sin melding for en ugyldig enum-verdi er
    /// «The JSON value could not be converted to Kaffebar.Models.CreateOrderRequest.
    /// Path: $.size | LineNumber: 0 | BytePositionInLine: 85.» — den navngir ROT-typen,
    /// ikke enumen, og den lekker byte-posisjoner ut til klienten. Vi bytter den ut.
    ///
    /// Merk forskjellen fra Java-sporet: der kan feilhåndtereren lese de lovlige
    /// verdiene ut av den genererte enum-klassen og svare «må være en av SMALL, MEDIUM,
    /// LARGE». Her stopper System.Text.Json før modellen finnes, og
    /// deserialiseringsfeilen bærer ikke med seg måltypen. Se FASIT.md, oppgave 4.
    /// </remarks>
    private static string Message(ModelError error)
    {
        if (error.Exception is JsonException
            || error.ErrorMessage.StartsWith("The JSON value could not be converted", StringComparison.Ordinal))
        {
            return "har en ugyldig verdi.";
        }

        return string.IsNullOrWhiteSpace(error.ErrorMessage) ? "er ugyldig." : error.ErrorMessage;
    }

    private static string FieldName(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "request";
        }

        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
        return JsonNamingPolicy.CamelCase.ConvertName(name);
    }
}

/// <summary>Gjør et <see cref="ProblemDetails"/> om til et svar med riktig media type.</summary>
public static class ProblemResults
{
    public static ObjectResult AsResult(this ProblemDetails problem) =>
        new(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
}
