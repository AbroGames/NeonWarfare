using MessagePack;
using MessagePack.Formatters;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

// The MessagePack source generator and analyzer accept SaveFileName members of [MessagePackObject] types only with
// this.
[assembly: MessagePackKnownFormatter(typeof(SaveFileNameFormatter))]

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>Writes a <see cref="SaveFileName"/> as a bare string; nil reads as <c>default</c>.</summary>
public sealed class SaveFileNameFormatter : IMessagePackFormatter<SaveFileName>
{
    public static readonly SaveFileNameFormatter Instance = new();

    public void Serialize(ref MessagePackWriter writer, SaveFileName value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public SaveFileName Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new SaveFileName(reader.ReadString());
    }
}
