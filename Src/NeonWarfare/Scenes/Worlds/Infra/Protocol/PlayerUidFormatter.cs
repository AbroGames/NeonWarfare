using MessagePack;
using MessagePack.Formatters;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

// The MessagePack source generator and analyzer accept PlayerUid members of [MessagePackObject] types only with this.
[assembly: MessagePackKnownFormatter(typeof(PlayerUidFormatter))]

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>Writes a <see cref="PlayerUid"/> as a bare string; nil reads as <c>default</c>.</summary>
public sealed class PlayerUidFormatter : IMessagePackFormatter<PlayerUid>
{
    public static readonly PlayerUidFormatter Instance = new();

    public void Serialize(ref MessagePackWriter writer, PlayerUid value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public PlayerUid Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new PlayerUid(reader.ReadString());
    }
}
