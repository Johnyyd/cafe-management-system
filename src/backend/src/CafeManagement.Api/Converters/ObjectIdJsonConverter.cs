using MongoDB.Bson;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CafeManagement.Api.Converters;

public class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (!string.IsNullOrEmpty(stringValue) && ObjectId.TryParse(stringValue, out var objectId))
            {
                return objectId;
            }
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return ObjectId.Empty;
        }

        return ObjectId.Empty;
    }

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class NullableObjectIdJsonConverter : JsonConverter<ObjectId?>
{
    public override ObjectId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
                return null;
            if (ObjectId.TryParse(stringValue, out var objectId))
                return objectId;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, ObjectId? value, JsonSerializerOptions options)
    {
        if (value.HasValue && value.Value != ObjectId.Empty)
            writer.WriteStringValue(value.Value.ToString());
        else
            writer.WriteNullValue();
    }
}
