using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySMS.API.Functions.Converters;

public class EnumJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        // Handle numeric values
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var intValue))
            {
                if (Enum.IsDefined(typeof(TEnum), intValue))
                {
                    return (TEnum)(object)intValue;
                }
                throw new JsonException(
                    $"Value {intValue} is not a valid value for enum {typeof(TEnum).Name}."
                );
            }

            throw new JsonException("Unable to parse enum value from number.");
        }

        // Get the string value from the JSON

        var value = reader.GetString();

        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        // Attempt to parse the string into the enum
        if (Enum.TryParse<TEnum>(value, true, out var result))
        {
            return result;
        }

        // Handle unknown values
        throw new JsonException($"Unable to convert '{value}' to {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        // Serialize the enum value as a string
        writer.WriteStringValue(value.ToString());
    }
}
