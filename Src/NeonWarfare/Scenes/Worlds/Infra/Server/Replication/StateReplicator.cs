using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using RepliCAT;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Replication;

/// <summary>
/// Writes the state section of a tick update, the join snapshot and the save from one set of RepliCAT baselines, see
/// Docs/Networking.md#entities-and-replication.
/// The state section has three parts, each closed by <see cref="NetId.None"/>:
/// <list type="number">
/// <item>Spawns. For every entity spawned since the last write, in NetId order: its NetId, kind id and parent NetId
/// (varuints, <see cref="NetId.None"/> for the World root), a bit saying that the state follows, and its first
/// RepliCAT delta.</item>
/// <item>Models. For every other entity whose models changed: its NetId (varuint) and its RepliCAT delta.</item>
/// <item>Despawns. The NetId (varuint) of every entity despawned since the last write, descendants included.</item>
/// </list>
/// A delta carries no length, so the client must know every NetId in the section.
/// One section serves every peer, because writing a delta moves its baseline.
/// </summary>
[Server]
public class StateReplicator
{
    private readonly ILogger _log = LogFactory.GetForStatic<StateReplicator>();

    private readonly IEntityFinder _entities;
    private readonly Replicator _replicator;
    // Sorted by NetId, so the same world always gives the same packet.
    private readonly SortedDictionary<long, Entry> _entryById = new();
    // Sorted by NetId, so a parent spawned in the same tick comes before its child.
    private readonly SortedSet<long> _spawned = [];
    // The despawned entities keep their baselines until the next send, so the baselines always describe the entities
    // as they were at the last send.
    private readonly List<NetId> _despawned = [];
    // The entities whose last delta failed and whose baselines RepliCAT has reset. A snapshot can do without their
    // state, because their next delta is complete, but a save cannot.
    private readonly SortedSet<long> _stateLost = [];

    // The kind and the parent are kept from the spawn, because a despawned entity may leave the registry before the
    // next send.
    private record Entry(ReplicationBaseline Baseline, int KindId, NetId Parent, bool Saved);

    public StateReplicator(IEntityFinder entities, Replicator replicator)
    {
        _entities = entities;
        _replicator = replicator;
        entities.Spawned += OnEntitySpawned;
        entities.Despawned += OnEntityDespawned;
    }

    /// <summary>
    /// Writes the state section of the tick. Called every tick, even when no peer gets it.
    /// That keeps the baselines equal to the state at the end of the last tick, and a joining peer's snapshot is
    /// built from them.
    /// </summary>
    /// <returns>
    /// <c>false</c> when nothing was spawned, changed or despawned. The writer is then left as it was, and nothing is
    /// sent.
    /// </returns>
    public bool TryWrite(BitWriter writer)
    {
        _despawned.ForEach(id =>
        {
            _entryById.Remove(id.Value);
            _stateLost.Remove(id.Value);
        });

        int start = writer.BitPosition;

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

    /// <summary>
    /// Writes every entity as it was at the last send, in NetId order, closed by <see cref="NetId.None"/>.
    /// Each entity gets the same record as in the spawns of the state section.
    /// <list type="bullet">
    /// <item>It must be called right after <see cref="TryWrite"/>, before anything spawns.
    /// It is built from the baselines, not from the live entities, so the next state section continues it
    /// consistently.</item>
    /// <item>An entity despawned since the send is still in it, and the next state section despawns it.</item>
    /// <item>An entity whose delta was never written goes without state. Its full state comes with its next
    /// delta.</item>
    /// </list>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// An entity has spawned since the last send: it has no state yet, and the next state section would spawn it
    /// again.
    /// </exception>
    /// <exception cref="ReplicationException">A manual member cannot be written.</exception>
    public void WriteSnapshot(BitWriter writer) => WriteAll(writer, saving: false);

    /// <summary>
    /// Writes the records of <see cref="WriteSnapshot"/>, except that a <see cref="NotSavedAttribute"/> entity goes
    /// without state.
    /// It may be called between ticks too. An entity that has left the tree since the send is still written from its
    /// baseline.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// An entity has spawned since the last send, or a saved entity has had no successful delta since its last failed
    /// one.
    /// </exception>
    /// <exception cref="ReplicationException">A manual member cannot be written.</exception>
    public void WriteSave(BitWriter writer)
    {
        List<long> lost = _stateLost.Where(id => _entryById[id].Saved).ToList();
        if (lost.Count > 0)
        {
            throw new InvalidOperationException($"The state of {string.Join(", ", lost)} was lost with a failed "
                                                + "delta: a save without it would load them empty.");
        }
        WriteAll(writer, saving: true);
    }

    private void WriteAll(BitWriter writer, bool saving)
    {
        if (_spawned.Count > 0)
        {
            throw new InvalidOperationException($"{_spawned.Count} entities spawned since the last send: the "
                                                + "snapshot is written right after TryWrite.");
        }

        foreach ((long id, Entry entry) in _entryById)
        {
            WriteSpawnRecord(id, entry, writer,
                () => (!saving || entry.Saved) && _replicator.TryWriteSnapshot(entry.Baseline, writer));
        }
        writer.WriteVarUInt((ulong) NetId.None.Value);
    }

    private void WriteSpawns(BitWriter writer)
    {
        foreach (long id in _spawned)
        {
            // A record goes without state when the kind has no replicated member or the first delta threw.
            // After a throw RepliCAT has reset the baseline, so the next packet carries the full state.
            WriteSpawnRecord(id, _entryById[id], writer, () => TryWriteDelta(new NetId(id), writer));
        }
        writer.WriteVarUInt((ulong) NetId.None.Value);
    }

    private static void WriteSpawnRecord(long id, Entry entry, BitWriter writer, Func<bool> tryWriteState)
    {
        writer.WriteVarUInt((ulong) id);
        writer.WriteVarUInt((ulong) entry.KindId);
        writer.WriteVarUInt((ulong) entry.Parent.Value);

        int stateBit = writer.BitPosition;
        writer.WriteBool(true);
        if (!tryWriteState())
        {
            writer.Rewind(stateBit);
            writer.WriteBool(false);
        }
    }

    private bool WriteModels(BitWriter writer)
    {
        bool written = false;
        foreach (long id in _entryById.Keys)
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
            bool written = _replicator.TryWriteDelta(_entryById[id.Value].Baseline, writer);
            _stateLost.Remove(id.Value);
            return written;
        }
        catch (ReplicationException e)
        {
            // RepliCAT has reset the baseline, so the next delta of this entity is complete.
            _log.Error(e, "The delta of {netId} failed, the other entities are still written", id);
            _stateLost.Add(id.Value);
            return false;
        }
    }

    private void OnEntitySpawned(NetId id, Node node)
    {
        NetId parent = _entities.TryGetNetId(node.GetParent(), out NetId parentId) ? parentId : NetId.None;
        bool saved = !Attribute.IsDefined(node.GetType(), typeof(NotSavedAttribute), inherit: false);
        _entryById.Add(id.Value,
            new Entry(_replicator.CreateBaseline(node), _entities.GetKindId(id), parent, saved));
        _spawned.Add(id.Value);
    }

    // An entity spawned and despawned between two sends is dropped without a trace, because no client has heard of it.
    private void OnEntityDespawned(NetId id, Node node)
    {
        if (_spawned.Remove(id.Value))
        {
            _entryById.Remove(id.Value);
            return;
        }
        _despawned.Add(id);
    }
}
