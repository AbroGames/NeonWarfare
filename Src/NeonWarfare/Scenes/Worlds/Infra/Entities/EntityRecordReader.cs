using System;
using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// Spawns entities from the spawn records of <c>StateReplicator</c>, with the NetIds the records give them.
/// The records come from a state packet, a join snapshot or a save.
/// An entity gets its state before it enters the tree and the registry, as on the server's spawn, so its
/// <c>_Ready</c> and the registry's subscribers see it configured.
/// </summary>
public class EntityRecordReader(EntityRegistry registry, WorldRoot root, EntityCatalog catalog, Replicator replicator)
{
    /// <summary>
    /// Reads and spawns the records up to <see cref="NetId.None"/>.
    /// Not atomic: the records before a broken one stay spawned.
    /// </summary>
    /// <returns>The highest NetId read, or <see cref="NetId.None"/> when there is no record.</returns>
    /// <exception cref="NetMessageFormatException">A record is broken.</exception>
    /// <exception cref="ReplicationException">A state cannot be read or applied.</exception>
    public NetId ReadAll(ref BitReader reader)
    {
        NetId highest = NetId.None;
        for (NetId id = ReadNetId(ref reader); id != NetId.None; id = ReadNetId(ref reader))
        {
            Spawn(id, ref reader);
            if (id.Value > highest.Value) highest = id;
        }
        return highest;
    }

    public static NetId ReadNetId(ref BitReader reader) => new((long) reader.ReadVarUInt());

    private void Spawn(NetId id, ref BitReader reader)
    {
        ulong kindId = reader.ReadVarUInt();
        NetId parent = ReadNetId(ref reader);
        bool hasState = reader.ReadBool();
        if (registry.TryGetNode(id, out _))
        {
            throw new NetMessageFormatException($"The record spawns {id}, which is already registered.");
        }
        Node parentNode = null;
        if (parent != NetId.None && !registry.TryGetNode(parent, out parentNode))
        {
            throw new NetMessageFormatException($"The record spawns {id} under {parent}, which is not registered.");
        }

        Node node;
        try
        {
            node = catalog.Create(kindId > int.MaxValue ? -1 : (int) kindId);
        }
        catch (ArgumentException e)
        {
            throw new NetMessageFormatException(
                $"The record spawns {id} of kind {kindId}, which is not in the catalog.", e);
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
}
