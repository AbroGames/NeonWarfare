using NeonWarfare.Scenes.Worlds.Infra.Entities;
using RepliCAT;
using RepliCAT.Bits;
using RepliCAT.Codecs;

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>Replicates a <see cref="NetId"/> as a varuint, so the small ids of a young world take few bits.</summary>
public sealed class NetIdCodec : IReplicationCodec<NetId>
{
    public static readonly NetIdCodec Instance = new();

    public void Write(BitWriter writer, NetId value)
    {
        if (value.Value < 0)
        {
            throw new ReplicationException($"NetId cannot be negative, got {value.Value}.");
        }

        writer.WriteVarUInt((ulong) value.Value);
    }

    public NetId Read(ref BitReader reader)
    {
        ulong value = reader.ReadVarUInt();
        if (value > long.MaxValue)
        {
            throw new ReplicationFormatException($"NetId {value} is out of range.");
        }

        return new NetId((long) value);
    }

    public bool IsChanged(NetId lastSent, NetId current) => lastSent != current;
}
