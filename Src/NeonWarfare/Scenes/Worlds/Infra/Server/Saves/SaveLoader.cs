using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// Fills an empty world from the save of <see cref="SaveWriter"/>: every entity with its saved NetId, then the NetId
/// sequence and the tick counter where they were. A loaded entity is registered as a spawned one, so the first tick
/// replicates the whole world, as it does for a new one.
/// </summary>
[Server]
public class SaveLoader(
    EntityRecordReader records,
    NetIdGenerator netIds,
    ServerTickClock clock,
    ProtocolHash protocolHash)
{
    /// <summary>
    /// Not atomic: on an error the entities before the broken record are already spawned, and the World must be
    /// freed. A foreign save spawns nothing.
    /// </summary>
    /// <exception cref="SaveVersionMismatchException">The save was written with another protocol hash.</exception>
    /// <exception cref="SaveFormatException">The save is truncated or broken.</exception>
    public void Load(ReadOnlyMemory<byte> save)
    {
        if (save.Length * 8 < SaveWriter.HashBits)
        {
            throw new SaveFormatException($"The save is {save.Length} bytes long, shorter than its protocol hash.");
        }

        long next;
        long tick;
        try
        {
            var reader = new BitReader(save.Span);
            ulong hash = reader.ReadBits(SaveWriter.HashBits);
            if (hash != protocolHash.Value)
            {
                throw new SaveVersionMismatchException(hash, protocolHash.Value);
            }

            next = (long) reader.ReadVarUInt();
            tick = (long) reader.ReadVarUInt();
            if (tick < 0)
            {
                throw new SaveFormatException($"The save has tick {tick}.");
            }
            NetId highest = records.ReadAll(ref reader);
            if (highest.Value >= next || next < 1)
            {
                throw new SaveFormatException($"The save has {highest}, but its next NetId is {next}.");
            }
            if (reader.RemainingBits >= 8)
            {
                throw new SaveFormatException($"{reader.RemainingBits} bits left after the end of the save.");
            }
        }
        catch (Exception e) when (e is NetMessageFormatException or ReplicationException)
        {
            throw new SaveFormatException("The save is broken.", e);
        }

        netIds.Restore(next);
        clock.Restore(tick);
    }
}
