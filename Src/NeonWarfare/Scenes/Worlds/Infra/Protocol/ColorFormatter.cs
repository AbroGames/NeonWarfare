using Godot;
using MessagePack;
using MessagePack.Formatters;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

// Lets the MessagePack source generator and analyzer accept Color members of [MessagePackObject] types
[assembly: MessagePackKnownFormatter(typeof(ColorFormatter))]

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

public sealed class ColorFormatter : IMessagePackFormatter<Color>
{
    private const int ComponentCount = 4;

    public static readonly ColorFormatter Instance = new();

    public void Serialize(ref MessagePackWriter writer, Color value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(ComponentCount);
        writer.Write(value.R);
        writer.Write(value.G);
        writer.Write(value.B);
        writer.Write(value.A);
    }

    public Color Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        int count = reader.ReadArrayHeader();
        if (count != ComponentCount)
        {
            throw new MessagePackSerializationException(
                $"Color must be an array of {ComponentCount} floats, got {count} elements.");
        }

        return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
