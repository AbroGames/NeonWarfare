using System;
using System.Collections.Generic;
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
/// The client's side of the spawner: an entity is created empty from its kind, gets its state, then its place in the
/// tree and in the registry — the order of the server's spawn, so <c>_Ready</c> and the registry's subscribers see it
/// configured.
/// </summary>
[ClientReplication]
public class StateApplier(EntityRegistry registry, WorldRoot root, EntityCatalog catalog, Replicator replicator)
{
    private const string EmptyPacketError = "The packet is empty.";
    private const string NotStatePacketError = "The packet kind is {0}, not a state packet.";
    private const string SpawnTakenError = "The state packet spawns {0}, which is already registered.";
    private const string UnknownKindError = "The state packet spawns {0} of kind {1}, which is not in the catalog.";
    private const string UnknownParentError = "The state packet spawns {0} under {1}, which is not registered.";
    private const string UnknownNetIdError = "The state packet has a delta for {0}, which is not registered.";
    private const string UnknownDespawnError = "The state packet despawns {0}, which is not registered.";
    private const string BrokenPacketError = "The state packet is broken.";
    private const string TrailingBitsError = "{0} bits left after the end of the state packet.";

    /// <summary>
    /// Not atomic: the records before a broken one are already applied.
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
            for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
            {
                Spawn(id, ref reader);
            }
            for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
            {
                if (!registry.TryGetNode(id, out Node node))
                {
                    throw new NetMessageFormatException(UnknownNetIdError.FormatWith(id));
                }
                replicator.Apply(node, ref reader);
            }
            Despawn(ref reader);

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

    private void Spawn(NetId id, ref BitReader reader)
    {
        ulong kindId = reader.ReadVarUInt();
        NetId parent = ReadNetId(ref reader);
        bool hasState = reader.ReadBool();
        if (registry.TryGetNode(id, out _))
        {
            throw new NetMessageFormatException(SpawnTakenError.FormatWith(id));
        }
        Node parentNode = null;
        if (parent != NetId.None && !registry.TryGetNode(parent, out parentNode))
        {
            throw new NetMessageFormatException(UnknownParentError.FormatWith(id, parent));
        }

        Node node;
        try
        {
            node = catalog.Create(kindId > int.MaxValue ? -1 : (int) kindId);
        }
        catch (ArgumentException e)
        {
            throw new NetMessageFormatException(UnknownKindError.FormatWith(id, kindId), e);
        }

        if (hasState)
        {
            try
            {
                replicator.Apply(node, ref reader);
            }
            catch
            {
                node.Free();
                throw;
            }
        }

        if (parentNode == null)
        {
            root.AddChild(node);
        }
        else
        {
            parentNode.AddChild(node);
        }
        registry.Register(id, node, (int) kindId);
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

    private static NetId ReadNetId(ref BitReader reader) => new((long) reader.ReadVarUInt());
}
