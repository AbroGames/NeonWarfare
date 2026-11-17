using System;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.World.Infra.ClientReplication;

/// <summary>
/// Applies the state packet of <c>StateReplicator</c> to the entities of a remote client, as soon as it arrives:
/// the events packet of the same tick follows it on the same channel, so its handlers see the models already updated.
/// </summary>
[ClientReplication]
public class StateApplier(IEntityFinder entities, Replicator replicator)
{
    private const string EmptyPacketError = "The packet is empty.";
    private const string NotStatePacketError = "The packet kind is {0}, not a state packet.";
    private const string UnknownNetIdError = "The state packet has a delta for {0}, which is not registered.";
    private const string BrokenPacketError = "The state packet is broken.";
    private const string TrailingBitsError = "{0} bits left after the end of the state packet.";

    /// <summary>
    /// Not atomic: the deltas before a broken one are already applied.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The packet is not a state packet or is broken.</exception>
    public void ApplyPacket(ReadOnlyMemory<byte> packet)
    {
        ReadOnlySpan<byte> span = packet.Span;
        if (span.IsEmpty)
        {
            throw new NetMessageFormatException(EmptyPacketError);
        }
        if (span[0] != (byte) ServerPacketKind.State)
        {
            throw new NetMessageFormatException(NotStatePacketError.FormatWith(span[0]));
        }

        try
        {
            var reader = new BitReader(span[1..]);
            //TODO TickTimer: keep the tick number
            reader.ReadVarUInt();
            for (ulong id = reader.ReadVarUInt(); id != (ulong) NetId.None.Value; id = reader.ReadVarUInt())
            {
                var netId = new NetId((long) id);
                if (!entities.TryGetNode(netId, out Node node))
                {
                    throw new NetMessageFormatException(UnknownNetIdError.FormatWith(netId));
                }
                replicator.Apply(node, ref reader);
            }

            if (reader.RemainingBits >= 8)
            {
                throw new NetMessageFormatException(TrailingBitsError.FormatWith(reader.RemainingBits));
            }
        }
        // Not only the format: a model the client cannot build from the delta is as broken a packet
        catch (ReplicationException e)
        {
            throw new NetMessageFormatException(BrokenPacketError, e);
        }
    }
}
