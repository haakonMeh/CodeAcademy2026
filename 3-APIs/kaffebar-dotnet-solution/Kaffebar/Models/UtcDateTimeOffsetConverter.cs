using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kaffebar.Models;

/// <summary>
/// Skriver tidsstempler som <c>2026-09-13T09:41:12.004Z</c>.
/// </summary>
/// <remarks>
/// .NET skriver som standard <c>2026-09-13T09:41:12.0040000+00:00</c>. Det er gyldig
/// ISO-8601 og alle klienter forstår det, men referanse-implementasjonen og Java-fasiten
/// skriver <c>Z</c> med millisekunder. Når alle tre skriver likt, kan man diffe
/// <c>GET /orders</c> mellom dem uten støy — og det er en test vi faktisk bruker.
/// </remarks>
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime().ToString(Format));
}
