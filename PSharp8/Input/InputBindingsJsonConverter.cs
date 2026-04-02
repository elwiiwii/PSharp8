using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSharp8.Input;

internal sealed class InputBindingsJsonConverter : JsonConverter<InputBindings>
{
    public override InputBindings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = new Dictionary<PicoButton, IReadOnlyList<InputSource>>();

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for InputBindings.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name.");

            var keyStr = reader.GetString() ?? throw new JsonException("Property name was null.");
            if (!Enum.TryParse<PicoButton>(keyStr, ignoreCase: true, out var button))
                throw new JsonException($"Unknown PicoButton value: '{keyStr}'.");

            reader.Read();
            var sources = JsonSerializer.Deserialize<List<InputSource>>(ref reader, options)
                ?? throw new JsonException("InputSource list was null.");
            dict[button] = sources;
        }

        return new InputBindings(dict);
    }

    public override void Write(Utf8JsonWriter writer, InputBindings value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (PicoButton button in Enum.GetValues<PicoButton>())
        {
            var sources = value[button];
            if (sources.Count == 0)
                continue;

            writer.WritePropertyName(button.ToString());
            JsonSerializer.Serialize(writer, (IReadOnlyList<InputSource>)sources, options);
        }
        writer.WriteEndObject();
    }
}
