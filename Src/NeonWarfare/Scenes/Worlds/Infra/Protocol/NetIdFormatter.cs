using MessagePack;
using MessagePack.Formatters;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

// The MessagePack source generator and analyzer accept NetId members of [MessagePackObject] types only with this.
[assembly: MessagePackKnownFormatter(typeof(NetIdFormatter))]

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>Writes a <see cref="NetId"/> as a bare integer.</summary>
public sealed class NetIdFormatter : IMessagePackFormatter<NetId>
{
    public static readonly NetIdFormatter Instance = new();

    public void Serialize(ref MessagePackWriter writer, NetId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public NetId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        long value = reader.ReadInt64();
        if (value < 0)
        {
            throw new MessagePackSerializationException($"NetId cannot be negative, got {value}.");
        }

        return new NetId(value);
    }
}
