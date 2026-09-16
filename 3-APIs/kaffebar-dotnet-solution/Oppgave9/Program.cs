using System.Text.Json;
using System.Text.Json.Serialization;
using Oppgave9;
using Scalar.AspNetCore;

// Oppgave 9 — polymorfisme, som et selvstendig, kjørbart eksempel.
//
//   dotnet run --project Oppgave9 -- --selftest   # serialisering + deserialisering
//   dotnet run --project Oppgave9                 # API på http://localhost:5036
//                                                 # /openapi/v1.json og /scalar/v1
//
// HVORFOR EGET PROSJEKT: legger du [JsonPolymorphic] inn i Kaffebar/Models/Orders.cs,
// endrer du formen på Order. Da brekker wire-kompatibiliteten med
// 4-Frontend/kaffebar-api, og den genererte TypeScript-klienten i samling 4 må skrives
// om. Referanse-implementasjonen har utelatt oppgave 9 av samme grunn.
//
// Se NOTAT.md for hva /openapi/v1.json faktisk ble.

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter() },
    WriteIndented = true
};

if (args.Contains("--selftest"))
{
    return SelfTest.Run(jsonOptions);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapScalarApiReference();

// Ekko-endepunkt: send inn en blandet ordre, få den tilbake som en MixedOrder.
// Poenget er ikke logikken, det er hva /openapi/v1.json sier om `items`.
app.MapPost("/orders-v2", (CreateMixedOrderRequest request) => Results.Created(
        $"/orders-v2/{Guid.NewGuid()}",
        new MixedOrder(Guid.NewGuid(), request.CustomerName, request.Items,
            OrderStatus.Pending, DateTimeOffset.UtcNow)))
    .WithName("CreateMixedOrder")
    .WithSummary("Opprett bestilling med blandede varelinjer")
    .Produces<MixedOrder>(StatusCodes.Status201Created);

app.Run();
return 0;
