using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.Worlds.Infra.Client.Replication;

/// <summary>
/// Applies the state packets and the join snapshot of <c>StateReplicator</c> on a remote client, see
/// Docs/Networking.md#packets-server--client.
/// </summary>
[ClientReplication]
public class StateApplier(EntityRegistry registry, EntityRecordReader records, Replicator replicator)
{
    /// <summary>Not atomic: the records before a broken one stay applied.</summary>
    /// <exception cref="NetMessageFormatException">The body is broken.</exception>
    public void ApplyState(ReadOnlyMemory<byte> body)
    {
        Read(body, (ref BitReader reader) =>
        {
            records.ReadAll(ref reader);
            for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
            {
                if (!registry.TryGetNode(id, out Node node))
                {
                    throw new NetMessageFormatException(
                        $"The state packet has a delta for {id}, which is not registered.");
                }
                replicator.Apply(node, ref reader);
            }
            Despawn(ref reader);
        });
    }

    /// <summary>
    /// Spawns every entity of the world from the join snapshot. Not atomic either.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The snapshot is broken.</exception>
    public void ApplySnapshot(ReadOnlyMemory<byte> body)
    {
        Read(body, (ref BitReader reader) => records.ReadAll(ref reader));
    }

    private delegate void BodyReader(ref BitReader reader);

    private static void Read(ReadOnlyMemory<byte> body, BodyReader readBody)
    {
        try
        {
            var reader = new BitReader(body.Span);
            // TODO Keep the tick number for the TickTimer.
            reader.ReadVarUInt();
            readBody(ref reader);

            if (reader.RemainingBits >= 8)
            {
                throw new NetMessageFormatException($"{reader.RemainingBits} bits left after the end of the packet.");
            }
        }
        // A model the client cannot build from the delta makes the packet as broken as a format error does.
        catch (ReplicationException e)
        {
            throw new NetMessageFormatException("The packet is broken.", e);
        }
    }

    // Every NetId is checked before the first removal. A descendant listed after its ancestor is skipped, because it
    // has already left with the ancestor.
    private void Despawn(ref BitReader reader)
    {
        List<NetId> despawned = [];
        for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
        {
            if (!registry.TryGetNode(id, out _))
            {
                throw new NetMessageFormatException($"The state packet despawns {id}, which is not registered.");
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
