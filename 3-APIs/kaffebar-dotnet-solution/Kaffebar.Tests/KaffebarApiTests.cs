using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Kaffebar.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Kaffebar.Tests;

/// <summary>
/// Testene som holder kontrakten ærlig.
/// </summary>
/// <remarks>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> starter hele appen i minnet — med
/// ekte ruting, ekte modellbinding, ekte validering og ekte serialisering — og gir deg
/// en <see cref="HttpClient"/> mot den. Det er poenget: en test som kaller
/// <c>controller.CreateOrder(...)</c> direkte ville ikke fanget noe av det som faktisk
/// kan gå galt her. Statuskoder, Location-headeren, media typen på feilsvar og om
/// enums kommer ut som strenger er alle egenskaper ved HTTP-laget.
/// </remarks>
public class KaffebarApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string Latte = "c0ffee01-0000-4000-8000-000000000007";
    private const string ProblemJson = "application/problem+json";

    /// <summary>Fra seed-dataene: Jonas sin espresso, status PENDING.</summary>
    private const string SeededPending = "0de50001-0000-4000-8000-000000000003";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public KaffebarApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Lagringen er i minne og delt mellom testene. Uten denne ville rekkefølgen på
        // testene bestemt om de går grønt.
        factory.Services.GetRequiredService<IOrderRepository>().Reset();
    }

    public void Dispose() => _client.Dispose();

    // ---------------------------------------------------------------- meny --

    [Fact(DisplayName = "GET /menu gir de ti faste varene med faste uuid-er")]
    public async Task Menu_ReturnsTenFixedItems()
    {
        var menu = await _client.GetFromJsonAsync<JsonElement>("/menu");

        Assert.Equal(10, menu.GetArrayLength());
        Assert.Equal("c0ffee01-0000-4000-8000-000000000001", menu[0].GetProperty("id").GetString());
        Assert.Equal("Filterkaffe", menu[0].GetProperty("name").GetString());
        Assert.Equal(39, menu[0].GetProperty("price").GetDecimal());
        Assert.Equal("c0ffee01-0000-4000-8000-00000000000a", menu[9].GetProperty("id").GetString());
    }

    // ------------------------------------------------------ POST /orders ----

    [Fact(DisplayName = "POST /orders gir 201 med Location-header")]
    public async Task CreateOrder_Returns201WithLocation()
    {
        var response = await PostJson("/orders", """
            {"coffeeId":"%ID%","customerName":"Ada","size":"MEDIUM","milkType":"OAT",
             "extraShot":true,"quantity":2}
            """);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/orders/", response.Headers.Location!.OriginalString);

        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PENDING", order.GetProperty("status").GetString());
        Assert.Equal("Kaffe Latte", order.GetProperty("coffeeName").GetString());
        Assert.Equal(2, order.GetProperty("quantity").GetInt32());
        Assert.Equal("OAT", order.GetProperty("milkType").GetString());
        Assert.True(order.GetProperty("extraShot").GetBoolean());
    }

    [Fact(DisplayName = "quantity defaulter til 1, og valgfrie felt uteblir helt")]
    public async Task CreateOrder_DefaultsQuantity()
    {
        var response = await PostJson("/orders",
            """{"coffeeId":"%ID%","customerName":"Ada","size":"SMALL"}""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, order.GetProperty("quantity").GetInt32());
        Assert.False(order.TryGetProperty("milkType", out _));
        Assert.False(order.TryGetProperty("extraShot", out _));
    }

    [Fact(DisplayName = "blankt navn gir 400 med problem+json og en liste over feltene")]
    public async Task CreateOrder_RejectsBlankName()
    {
        var response = await PostJson("/orders",
            """{"coffeeId":"%ID%","customerName":"  ","size":"SMALL","quantity":99}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("validation-error", problem.GetProperty("type").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("/orders", problem.GetProperty("instance").GetString());

        var fields = problem.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetProperty("field").GetString())
            .Distinct()
            .ToList();

        Assert.Contains("customerName", fields);
        Assert.Contains("quantity", fields);
    }

    [Fact(DisplayName = "ugyldig enum-verdi gir 400 med feltnavnet")]
    public async Task CreateOrder_RejectsUnknownEnumValue()
    {
        var response = await PostJson("/orders",
            """{"coffeeId":"%ID%","customerName":"Ada","size":"HUGE"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var fields = problem.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetProperty("field").GetString());

        Assert.Contains("size", fields);
    }

    [Fact(DisplayName = "kaffe som ikke finnes gir 404, ikke 400")]
    public async Task CreateOrder_RejectsUnknownCoffee()
    {
        var response = await PostJson("/orders", """
            {"coffeeId":"00000000-0000-4000-8000-000000000000","customerName":"Ada","size":"SMALL"}
            """);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    // ------------------------------------------------- GET /orders/{id} -----

    [Fact(DisplayName = "GET /orders/{id} gir 200 for en ordre som finnes")]
    public async Task GetOrder_ReturnsSeededOrder()
    {
        var order = await _client.GetFromJsonAsync<JsonElement>($"/orders/{SeededPending}");

        Assert.Equal("Jonas", order.GetProperty("customerName").GetString());
        Assert.Equal("PENDING", order.GetProperty("status").GetString());
    }

    [Fact(DisplayName = "ukjent ordre gir 404 med Problem Details")]
    public async Task GetOrder_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/orders/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("not-found", problem.GetProperty("type").GetString());
        Assert.Equal("Ikke funnet", problem.GetProperty("title").GetString());
    }

    /// <remarks>
    /// Oppgave 5. Dette er den bevisste forskjellen fra Java-sporet, som svarer 400.
    /// Route-constrainten <c>{orderId:guid}</c> gjør at ruten ikke matcher i det hele
    /// tatt, og da finnes det ikke noe endepunkt å gi en 400 fra. Se FASIT.md.
    /// </remarks>
    [Fact(DisplayName = "ugyldig guid gir 404 i .NET (Java-sporet gir 400)")]
    public async Task GetOrder_InvalidGuid_Returns404()
    {
        var response = await _client.GetAsync("/orders/ikke-en-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    // ----------------------------------------------------- GET /orders ------

    [Fact(DisplayName = "GET /orders gir alle fire seed-ordrene, nyeste først")]
    public async Task ListOrders_ReturnsNewestFirst()
    {
        var orders = await _client.GetFromJsonAsync<JsonElement>("/orders");

        Assert.Equal(4, orders.GetArrayLength());
        Assert.Equal("Ingrid", orders[0].GetProperty("customerName").GetString());
        Assert.Equal("Ada", orders[3].GetProperty("customerName").GetString());
    }

    [Fact(DisplayName = "GET /orders filtrerer på status")]
    public async Task ListOrders_FiltersByStatus()
    {
        var orders = await _client.GetFromJsonAsync<JsonElement>("/orders?status=PENDING");

        Assert.Equal(2, orders.GetArrayLength());
        Assert.All(orders.EnumerateArray(),
            order => Assert.Equal("PENDING", order.GetProperty("status").GetString()));
    }

    [Fact(DisplayName = "GET /orders pagineres med limit og offset")]
    public async Task ListOrders_Paginates()
    {
        var orders = await _client.GetFromJsonAsync<JsonElement>("/orders?limit=2&offset=1");

        Assert.Equal(2, orders.GetArrayLength());
        Assert.Equal("Jonas", orders[0].GetProperty("customerName").GetString());
    }

    [Fact(DisplayName = "limit over 100 gir 400 — grensen står i [Range], ikke i koden")]
    public async Task ListOrders_RejectsTooLargeLimit()
    {
        var response = await _client.GetAsync("/orders?limit=200");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("limit", problem.GetProperty("errors")[0].GetProperty("field").GetString());
    }

    [Fact(DisplayName = "ukjent status i query gir 400")]
    public async Task ListOrders_RejectsUnknownStatus()
    {
        var response = await _client.GetAsync("/orders?status=NOPE");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ------------------------------------------------------ statusendring ---

    [Fact(DisplayName = "PATCH oppdaterer status")]
    public async Task Patch_UpdatesStatus()
    {
        var response = await PatchJson($"/orders/{SeededPending}", """{"status":"BREWING"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BREWING", order.GetProperty("status").GetString());
        Assert.Equal(SeededPending, order.GetProperty("orderId").GetString());
    }

    [Fact(DisplayName = "POST /status gir nøyaktig det samme som PATCH")]
    public async Task Alias_BehavesIdentically()
    {
        var viaPatch = await (await PatchJson($"/orders/{SeededPending}", """{"status":"READY"}"""))
            .Content.ReadAsStringAsync();

        _factory.Services.GetRequiredService<IOrderRepository>().Reset();

        var viaAlias = await (await PostJson($"/orders/{SeededPending}/status", """{"status":"READY"}"""))
            .Content.ReadAsStringAsync();

        // Tidsstemplene settes i det øyeblikket kallet kommer inn, og seed-dataene er
        // relative til oppstart, så de to kan aldri bli bit for bit like.
        Assert.Equal(WithoutTimestamps(viaPatch), WithoutTimestamps(viaAlias));
    }

    [Fact(DisplayName = "statusendring på ukjent ordre gir 404")]
    public async Task Patch_UnknownOrder_ReturnsNotFound()
    {
        var response = await PatchJson(
            "/orders/00000000-0000-0000-0000-000000000000", """{"status":"READY"}""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact(DisplayName = "ukjent status gir 400")]
    public async Task Patch_UnknownStatus_ReturnsBadRequest()
    {
        var response = await PatchJson($"/orders/{SeededPending}", """{"status":"DONE"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ------------------------------------------------------ serialisering ---

    [Fact(DisplayName = "enums serialiseres som strenger, ikke som tall")]
    public async Task Enums_AreSerializedAsStrings()
    {
        var body = await _client.GetStringAsync("/orders");

        Assert.Contains("\"status\":\"PENDING\"", body);
        Assert.Contains("\"size\":\"MEDIUM\"", body);
        Assert.Contains("\"milkType\":\"SOY\"", body);
        Assert.DoesNotContain("\"status\":0", body);
    }

    [Fact(DisplayName = "GET /health rapporterer rabbitmq = disabled når hendelser er av")]
    public async Task Health_ReportsDisabled()
    {
        var health = await _client.GetFromJsonAsync<JsonElement>("/health");

        Assert.Equal("ok", health.GetProperty("status").GetString());
        Assert.Equal("disabled", health.GetProperty("rabbitmq").GetString());
    }

    // ------------------------------------------------------------ hjelpere --

    private Task<HttpResponseMessage> PostJson(string url, string json) =>
        _client.PostAsync(url, Json(json));

    private Task<HttpResponseMessage> PatchJson(string url, string json) =>
        _client.PatchAsync(url, Json(json));

    private static StringContent Json(string json) =>
        new(json.Replace("%ID%", Latte), Encoding.UTF8, "application/json");

    private static string WithoutTimestamps(string json) =>
        System.Text.RegularExpressions.Regex.Replace(
            json, "\"(createdAt|updatedAt)\":\"[^\"]+\"", "\"$1\":\"…\"");
}
