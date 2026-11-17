using Godot;
using Humanizer;
using MessagePack;
using MessagePack.Formatters;
using NeonWarfare.Scenes.World.Infra.Protocol;

// Lets the MessagePack source generator and analyzer accept Color members of [MessagePackObject] types
[assembly: MessagePackKnownFormatter(typeof(ColorFormatter))]

namespace NeonWarfare.Scenes.World.Infra.Protocol;

public sealed class ColorFormatter : IMessagePackFormatter<Color>
{
    private const int ComponentCount = 4;
    private const string WrongLengthError = "Color must be an array of {0} floats, got {1} elements.";

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
            throw new MessagePackSerializationException(WrongLengthError.FormatWith(ComponentCount, count));
        }

        return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
