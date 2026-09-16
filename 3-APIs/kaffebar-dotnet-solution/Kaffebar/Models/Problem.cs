namespace Kaffebar.Models;

/// <summary>
/// Ett felt som ikke validerte. Oppgave 4 og 6.
/// </summary>
/// <remarks>
/// .NET sin innebygde <c>ValidationProblemDetails</c> bruker en ORDBOK
/// (<c>"errors": { "CustomerName": ["..."] }</c>), mens kontrakten i
/// 4-Frontend/kaffebar-api bruker en LISTE av objekter
/// (<c>"errors": [{ "field": "...", "message": "..." }]</c>).
///
/// Vi følger kontrakten. Se <c>ProblemFactory</c> og FASIT.md, oppgave 4 — det er et
/// av de stedene code-first koster deg noe, og det er verdt en slide.
/// </remarks>
public record ValidationError(string Field, string Message);
