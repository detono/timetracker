using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeTracker.API.Converters;

public class TolerantTimeOnlyConverter : JsonConverter<TimeOnly> {
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();

        // This will happily parse both "08:30" and "08:30:00"
        if (TimeOnly.TryParse(value, out var time)) {
            return time;
        }

        throw new JsonException($"Unable to parse '{value}' as a TimeOnly.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) {
        // Standard ISO-8601 format going back out to the client
        writer.WriteStringValue(value.ToString("HH:mm:ss"));
    }
}