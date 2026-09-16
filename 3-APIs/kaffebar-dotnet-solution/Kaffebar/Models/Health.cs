using System.Text.Json.Serialization;

namespace Kaffebar.Models;

/// <summary>Den eneste lovlige verdien er «ok» — svarer tjenesten i det hele tatt, lever den.</summary>
public enum HealthStatus
{
    [JsonStringEnumMemberName("ok")] Ok
}

/// <summary>Tilstanden på RabbitMQ-tilkoblingen. API-et fungerer uansett verdi.</summary>
public enum RabbitmqState
{
    [JsonStringEnumMemberName("connected")] Connected,
    [JsonStringEnumMemberName("disconnected")] Disconnected,
    [JsonStringEnumMemberName("disabled")] Disabled
}

/// <summary>
/// Helsesjekk. Ikke en del av oppgavesettet, men en del av referansekontrakten —
/// Docker bruker den. Tatt med her slik at de tre implementasjonene eksponerer
/// nøyaktig de samme endepunktene.
/// </summary>
public record Health(HealthStatus Status, RabbitmqState Rabbitmq, double UptimeSeconds);
