using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace NeonWarfare.Scripts.GlobalServices.Settings;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string hex = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        // A JsonException, so that a bad color makes the file corrupt instead of crashing its reader
        if (hex == null || !Color.HtmlIsValid(hex)) throw new JsonException($"Not an HTML color: {hex}");
        return Color.FromHtml(hex);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string hex = value.ToHtml();
        writer.WriteStringValue(hex);
    }
}