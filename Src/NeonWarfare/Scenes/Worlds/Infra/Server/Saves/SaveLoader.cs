using System;
using Humanizer;
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
    NetMessageCodec codec)
{
    private const string TooShortError = "The save is {0} bytes long, shorter than its protocol hash.";
    private const string NetIdOutOfRangeError = "The save has {0}, but its next NetId is {1}.";
    private const string NegativeTickError = "The save has tick {0}.";
    private const string TrailingBitsError = "{0} bits left after the end of the save.";
    private const string BrokenError = "The save is broken.";

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
            throw new SaveFormatException(TooShortError.FormatWith(save.Length));
        }

        long next;
        long tick;
        try
        {
            var reader = new BitReader(save.Span);
            ulong hash = reader.ReadBits(SaveWriter.HashBits);
            if (hash != codec.ProtocolHash)
            {
                throw new SaveVersionMismatchException(hash, codec.ProtocolHash);
            }

            next = (long) reader.ReadVarUInt();
            tick = (long) reader.ReadVarUInt();
            if (tick < 0)
            {
                throw new SaveFormatException(NegativeTickError.FormatWith(tick));
            }
            NetId highest = records.ReadAll(ref reader);
            if (highest.Value >= next || next < 1)
            {
                throw new SaveFormatException(NetIdOutOfRangeError.FormatWith(highest, next));
            }
            if (reader.RemainingBits >= 8)
            {
                throw new SaveFormatException(TrailingBitsError.FormatWith(reader.RemainingBits));
            }
        }
        catch (NetMessageFormatException e)
        {
            throw new SaveFormatException(BrokenError, e);
        }
        catch (ReplicationException e)
        {
            throw new SaveFormatException(BrokenError, e);
        }

        netIds.Restore(next);
        clock.Restore(tick);
    }
}
