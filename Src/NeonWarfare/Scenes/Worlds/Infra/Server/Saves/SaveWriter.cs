using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Replication;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// Writes the save: the protocol hash (<see cref="HashBits"/>), the next NetId and the tick (varuints), then the
/// records of <see cref="StateReplicator.WriteSave"/>. The hash comes first and fixed-size, so a build of any later
/// format can still tell a foreign save before it reads anything else.
/// </summary>
[Server]
public class SaveWriter(StateReplicator stateReplicator, NetIdGenerator netIds, ProtocolHash protocolHash)
{
    public const int HashBits = 64;

    /// <summary>
    /// The save right now. Valid only between ticks, when the baselines are the end of the last one.
    /// </summary>
    /// <exception cref="InvalidOperationException">No tick has been sent since an entity spawned.</exception>
    /// <exception cref="ReplicationException">A manual member cannot be written.</exception>
    public byte[] Write()
    {
        var writer = new BitWriter();
        writer.WriteBits(protocolHash.Value, HashBits);
        writer.WriteVarUInt((ulong) netIds.NextValue);
        writer.WriteVarUInt((ulong) stateReplicator.LastSentTick);
        stateReplicator.WriteSave(writer);
        return writer.ToArray();
    }
}
