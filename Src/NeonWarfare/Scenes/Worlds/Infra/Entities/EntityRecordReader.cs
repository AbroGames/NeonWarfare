using System;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// Spawns entities with the NetIds the records give them: the spawn records of <c>StateReplicator</c>, from a state
/// packet, a join snapshot or a save. An entity is created empty from its kind, gets its state, then its place in the
/// tree and in the registry — the order of the server's spawn, so <c>_Ready</c> and the registry's subscribers see it
/// configured.
/// </summary>
public class EntityRecordReader(EntityRegistry registry, WorldRoot root, EntityCatalog catalog, Replicator replicator)
{
    private const string SpawnTakenError = "The record spawns {0}, which is already registered.";
    private const string UnknownKindError = "The record spawns {0} of kind {1}, which is not in the catalog.";
    private const string UnknownParentError = "The record spawns {0} under {1}, which is not registered.";

    /// <summary>
    /// Reads and spawns the records up to <see cref="NetId.None"/>. Not atomic: the records before a broken one are
    /// already spawned.
    /// </summary>
    /// <returns>The highest NetId read, <see cref="NetId.None"/> for no record.</returns>
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
}
