using System.Collections.Generic;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Replication;

/// <summary>
/// Writes the state packet of the tick: the kind byte, the tick number (varuint), then three sections, each closed by
/// <see cref="NetId.None"/>:
/// <list type="number">
/// <item>spawns — for every entity spawned since the last packet, by NetId: its NetId, kind id and parent NetId
/// (varuints, <see cref="NetId.None"/> for the World root), a bit "the state follows" and its first RepliCAT
/// delta;</item>
/// <item>models — for every other entity whose models changed, its NetId (varuint) and its RepliCAT delta;</item>
/// <item>despawns — the NetId (varuint) of every entity despawned since the last packet, descendants included.</item>
/// </list>
/// A delta carries no length, so the client must know every NetId in the packet. The state is captured here, at the
/// end of the tick, not at spawn. One packet for every peer: a written delta has already moved its baseline.
/// </summary>
[ServerNetwork]
public class StateReplicator
{
    private const string DeltaFailedLog = "The delta of {netId} failed, the other entities are still written";

    private readonly ILogger _log = LogFactory.GetForStatic<StateReplicator>();

    private readonly IEntityFinder _entities;
    private readonly Replicator _replicator;
    // By NetId: the order is the same on every run, so the packet is too
    private readonly SortedDictionary<long, ReplicationBaseline> _baselineById = new();
    // By NetId, so a parent spawned in the same tick comes before its child
    private readonly SortedSet<long> _spawned = [];
    // Their baselines stay until the end of the tick: the set of baselines is the set of entities at the last send
    private readonly List<NetId> _despawned = [];

    public StateReplicator(IEntityFinder entities, Replicator replicator)
    {
        _entities = entities;
        _replicator = replicator;
        entities.SpawnedEvent += OnEntitySpawned;
        entities.DespawnedEvent += OnEntityDespawned;
    }

    /// <summary>
    /// Called every tick, whether anyone gets the packet or not: the baselines stay equal to the state at the end of
    /// the last tick, which is what a joining peer's snapshot is built from.
    /// </summary>
    /// <returns>
    /// <c>false</c> when nothing was spawned, changed or despawned: the writer is left as it was, and nothing is sent.
    /// </returns>
    public bool TryWrite(long tick, BitWriter writer)
    {
        _despawned.ForEach(id => _baselineById.Remove(id.Value));

        int start = writer.BitPosition;
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt((ulong) tick);

        bool spawnedOrDespawned = _spawned.Count > 0 || _despawned.Count > 0;
        WriteSpawns(writer);
        bool modelsChanged = WriteModels(writer);
        WriteDespawns(writer);
        _spawned.Clear();
        _despawned.Clear();

        if (!spawnedOrDespawned && !modelsChanged)
        {
            writer.Rewind(start);
            return false;
        }
        return true;
    }

    private void WriteSpawns(BitWriter writer)
    {
        foreach (long id in _spawned)
        {
            var netId = new NetId(id);
            Node node = _entities.GetNode(netId);
            NetId parent = _entities.TryGetNetId(node.GetParent(), out NetId parentId) ? parentId : NetId.None;
            writer.WriteVarUInt((ulong) id);
            writer.WriteVarUInt((ulong) _entities.GetKindId(netId));
            writer.WriteVarUInt((ulong) parent.Value);

            // No delta for a kind with no replicated member, nor for a first delta that threw: RepliCAT has reset the
            // baseline, so the models section of the next packet carries the full state
            int stateBit = writer.BitPosition;
            writer.WriteBool(true);
            if (!TryWriteDelta(netId, writer))
            {
                writer.Rewind(stateBit);
                writer.WriteBool(false);
            }
        }
        writer.WriteVarUInt((ulong) NetId.None.Value);
    }

    private bool WriteModels(BitWriter writer)
    {
        bool written = false;
        foreach (long id in _baselineById.Keys)
        {
            if (_spawned.Contains(id)) continue;

            int recordStart = writer.BitPosition;
            writer.WriteVarUInt((ulong) id);
            if (TryWriteDelta(new NetId(id), writer))
            {
                written = true;
                continue;
            }
            writer.Rewind(recordStart);
        }
        writer.WriteVarUInt((ulong) NetId.None.Value);
        return written;
    }

    private void WriteDespawns(BitWriter writer)
    {
        foreach (NetId id in _despawned)
        {
            writer.WriteVarUInt((ulong) id.Value);
        }
        writer.WriteVarUInt((ulong) NetId.None.Value);
    }

    private bool TryWriteDelta(NetId id, BitWriter writer)
    {
        try
        {
            return _replicator.TryWriteDelta(_baselineById[id.Value], writer);
        }
        catch (ReplicationException e)
        {
            // RepliCAT has reset the baseline, so the next delta of this entity is complete
            _log.Error(e, DeltaFailedLog, id);
            return false;
        }
    }

    private void OnEntitySpawned(NetId id, Node node)
    {
        _baselineById.Add(id.Value, _replicator.CreateBaseline(node));
        _spawned.Add(id.Value);
    }

    // Spawned and despawned before a send: no client has heard of it
    private void OnEntityDespawned(NetId id, Node node)
    {
        if (_spawned.Remove(id.Value))
        {
            _baselineById.Remove(id.Value);
            return;
        }
        _despawned.Add(id);
    }
}
