using System;
using System.Collections.Generic;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.Worlds.Infra.ClientReplication;

/// <summary>
/// Applies the state packet of <c>StateReplicator</c> to the entities of a remote client, as soon as it arrives:
/// the events packet of the same tick follows it on the same channel, so its handlers see the models already updated.
/// The client's side of the spawner is <see cref="EntityRecordReader"/>. The join snapshot is the same spawn records,
/// with every entity.
/// </summary>
[ClientReplication]
public class StateApplier(EntityRegistry registry, EntityRecordReader records, Replicator replicator)
{
    private const string EmptyPacketError = "The packet is empty.";
    private const string WrongKindError = "The packet kind is {0}, not {1}.";
    private const string UnknownNetIdError = "The state packet has a delta for {0}, which is not registered.";
    private const string UnknownDespawnError = "The state packet despawns {0}, which is not registered.";
    private const string BrokenPacketError = "The packet is broken.";
    private const string TrailingBitsError = "{0} bits left after the end of the packet.";

    /// <summary>
    /// Not atomic: the records before a broken one are already applied.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The packet is not a state packet or is broken.</exception>
    public void ApplyPacket(ReadOnlyMemory<byte> packet)
    {
        Read(packet, ServerPacketKind.State, (ref BitReader reader) =>
        {
            records.ReadAll(ref reader);
            for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
            {
                if (!registry.TryGetNode(id, out Node node))
                {
                    throw new NetMessageFormatException(UnknownNetIdError.FormatWith(id));
                }
                replicator.Apply(node, ref reader);
            }
            Despawn(ref reader);
        });
    }

    /// <summary>
    /// Spawns every entity of the world from the join snapshot. Not atomic either.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The packet is not a snapshot or is broken.</exception>
    public void ApplySnapshot(ReadOnlyMemory<byte> packet)
    {
        Read(packet, ServerPacketKind.Snapshot, (ref BitReader reader) => records.ReadAll(ref reader));
    }

    private delegate void BodyReader(ref BitReader reader);

    private static void Read(ReadOnlyMemory<byte> packet, ServerPacketKind kind, BodyReader readBody)
    {
        ReadOnlySpan<byte> span = packet.Span;
        if (span.IsEmpty)
        {
            throw new NetMessageFormatException(EmptyPacketError);
        }
        if (span[0] != (byte) kind)
        {
            throw new NetMessageFormatException(WrongKindError.FormatWith(span[0], kind));
        }

        try
        {
            var reader = new BitReader(span[1..]);
            //TODO TickTimer: keep the tick number
            reader.ReadVarUInt();
            readBody(ref reader);

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

    // Every NetId is checked before the first removal; a descendant listed after its ancestor is already gone with it
    private void Despawn(ref BitReader reader)
    {
        List<NetId> despawned = [];
        for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
        {
            if (!registry.TryGetNode(id, out _))
            {
                throw new NetMessageFormatException(UnknownDespawnError.FormatWith(id));
            }
            despawned.Add(id);
        }

        foreach (NetId id in despawned)
        {
            if (!registry.TryGetNode(id, out Node node)) continue;

            node.GetParent().RemoveChild(node);
            node.QueueFree();
        }
    }

    private static NetId ReadNetId(ref BitReader reader) => EntityRecordReader.ReadNetId(ref reader);
}
