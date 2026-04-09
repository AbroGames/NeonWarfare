using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.SettingsSystem;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        return Color.FromHtml(hex);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string hex = value.ToHtml();
        writer.WriteStringValue(hex);
    }
}