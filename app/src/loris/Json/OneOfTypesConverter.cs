using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loris.Json;

public class OneOfTypeConverter(Type[] allowedTypes) : JsonConverter<object>
{
    private readonly Type[] _allowedTypes = allowedTypes;

    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        foreach (var type in _allowedTypes)
        {
            try
            {
                var result = jsonDocument.RootElement.Deserialize(type, options);
                if (result is not null)
                {
                    return result;
                }
            }
            catch (JsonException)
            {
                //  we try until success
            }
        }
        throw new JsonException(
            $"Value {jsonDocument.RootElement.GetRawText()} did not matchy any allowed OneOf typers."
        );
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
