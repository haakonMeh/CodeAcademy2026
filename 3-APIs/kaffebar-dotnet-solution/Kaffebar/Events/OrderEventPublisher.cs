using System.Text.Json;
using System.Text.Json.Serialization;
using Kaffebar.Models;
using RabbitMQ.Client;

namespace Kaffebar.Events;

/// <summary>Meldingsformatet på exchangen.</summary>
/// <remarks>
/// Bit for bit likt det 4-Frontend/kaffebar-api publiserer, slik at
/// <c>/api/events</c>-route handleren i kaffebar-web ikke merker forskjell på hvilken
/// implementasjon som står bak.
/// </remarks>
public record KaffebarEvent(
    string Type,
    string Timestamp,
    Order Data,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    OrderStatus? PreviousStatus);

/// <summary>
/// VALGFRITT SISTE STEG — ikke en del av oppgavesettet i samling 3.
/// </summary>
/// <remarks>
/// Oppgavene sier ingenting om hendelser. Men peker en deltaker frontenden i samling 4
/// på sitt eget API fra mai, får hen bare steg 1 (data over HTTP), ikke steg 2 (to
/// vinduer som oppdaterer seg selv). Uten hendelser mangler halve poenget med
/// samlingen.
///
/// Er <c>Kaffebar:Events:Enabled</c> false — som er standard — gjør denne klassen
/// bokstavelig talt ingenting. Ingen tilkobling, ingen logglinjer, ingen endret
/// HTTP-oppførsel.
///
/// Samme exchange (<c>kaffebar</c>, topic, durable), samme routing keys
/// (<c>order.created</c>, <c>order.status-changed</c>) og samme meldingsformat som
/// 4-Frontend/kaffebar-api.
/// </remarks>
public sealed class OrderEventPublisher(
    EventOptions options,
    IHostApplicationLifetime lifetime,
    ILogger<OrderEventPublisher> logger) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Samme konvertere som HTTP-svarene. Hendelsen skal inneholde nøyaktig det
        // samme Order-objektet som GET /orders/{id} ville gitt — ellers får en
        // konsument to litt ulike sannheter om samme ordre.
        Converters = { new JsonStringEnumConverter(), new UtcDateTimeOffsetConverter() }
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitmqState State =>
        !options.Enabled ? RabbitmqState.Disabled
        : _channel is { IsOpen: true } ? RabbitmqState.Connected
        : RabbitmqState.Disconnected;

    public void OrderCreated(Order order) =>
        Publish("order.created", new KaffebarEvent("order.created", Timestamp(), order, null));

    public void OrderStatusChanged(Order order, OrderStatus previousStatus) =>
        Publish("order.status-changed",
            new KaffebarEvent("order.status-changed", Timestamp(), order, previousStatus));

    /// <summary>
    /// Publiserer i bakgrunnen. HTTP-svaret skal aldri vente på broker, og en broker som
    /// er nede skal aldri gjøre et kall som ellers gikk bra om til en 500. Hendelser er
    /// en bonus; bestillingen er jobben.
    /// </summary>
    private void Publish(string routingKey, KaffebarEvent message)
    {
        if (!options.Enabled)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var channel = await EnsureChannelAsync(lifetime.ApplicationStopping);
                var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

                await channel.BasicPublishAsync(
                    exchange: options.Exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: new BasicProperties
                    {
                        ContentType = "application/json",
                        DeliveryMode = DeliveryModes.Persistent
                    },
                    body: body,
                    cancellationToken: lifetime.ApplicationStopping);
            }
            catch (Exception exception)
            {
                await ResetAsync();
                logger.LogWarning("Klarte ikke å publisere {RoutingKey}: {Message}",
                    routingKey, exception.Message);
            }
        });
    }

    /// <summary>
    /// Kobler til ved behov og deklarerer exchangen hver gang en ny kanal lages. Da er
    /// exchangen garantert der før den første meldingen, også etter en reconnect.
    /// </summary>
    private async Task<IChannel> EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.Uri),
                AutomaticRecoveryEnabled = true
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic,
                durable: true, autoDelete: false, cancellationToken: cancellationToken);

            logger.LogInformation("Koblet til RabbitMQ. Exchange \"{Exchange}\" (topic, durable).",
                options.Exchange);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ResetAsync()
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch
        {
            // Broker kan allerede være borte. Det er greit.
        }
        finally
        {
            _channel = null;
            _connection = null;
        }
    }

    private static string Timestamp() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
        _gate.Dispose();
    }
}
