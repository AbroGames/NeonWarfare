using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using RepliCAT;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Replication;

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
/// The same records of every entity, without the packet header, make the snapshot: <see cref="WriteSnapshot"/>, and
/// the save: <see cref="WriteSave"/>.
/// </summary>
[ServerNetwork]
public class StateReplicator
{
    private const string DeltaFailedLog = "The delta of {netId} failed, the other entities are still written";
    private const string SpawnedSinceSendError =
        "{0} entities spawned since the last send: the snapshot is written right after TryWrite.";
    private const string StateLostError =
        "The state of {0} was lost with a failed delta: a save without it would load them empty.";

    private readonly ILogger _log = LogFactory.GetForStatic<StateReplicator>();

    private readonly IEntityFinder _entities;
    private readonly Replicator _replicator;
    // By NetId: the order is the same on every run, so the packet is too
    private readonly SortedDictionary<long, Entry> _entryById = new();
    // By NetId, so a parent spawned in the same tick comes before its child
    private readonly SortedSet<long> _spawned = [];
    // Their baselines stay until the end of the tick: the set of baselines is the set of entities at the last send
    private readonly List<NetId> _despawned = [];
    // A failed delta resets the baseline: a snapshot takes the state from the next delta, a save has none to take
    private readonly SortedSet<long> _stateLost = [];

    // Kind and parent are kept from the spawn: a despawned entity may be out of the registry before the next send
    private record Entry(ReplicationBaseline Baseline, int KindId, NetId Parent, bool Saved);

    public StateReplicator(IEntityFinder entities, Replicator replicator)
    {
        _entities = entities;
        _replicator = replicator;
        entities.SpawnedEvent += OnEntitySpawned;
        entities.DespawnedEvent += OnEntityDespawned;
    }

    /// <summary>
    /// The tick of the last <see cref="TryWrite"/>: the baselines are the state at its end.
    /// </summary>
    public long LastSentTick { get; private set; }

    /// <summary>
    /// Called every tick, whether anyone gets the packet or not: the baselines stay equal to the state at the end of
    /// the last tick, which is what a joining peer's snapshot is built from.
    /// </summary>
    /// <returns>
    /// <c>false</c> when nothing was spawned, changed or despawned: the writer is left as it was, and nothing is sent.
    /// </returns>
    public bool TryWrite(long tick, BitWriter writer)
    {
        _despawned.ForEach(id =>
        {
            _entryById.Remove(id.Value);
            _stateLost.Remove(id.Value);
        });
        LastSentTick = tick;

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

    /// <summary>
    /// Every entity as it was at the last send, by NetId: the same record as in the spawns section of the state
    /// packet, closed by <see cref="NetId.None"/>. No packet kind and no tick: the caller frames it. Written right
    /// after <see cref="TryWrite"/>, before anything spawns: "the snapshot, then the next state packet" is then
    /// consistent, because it is built from the baselines, not from the live entities. An entity despawned since the
    /// send is still in it, and the next packet despawns it. An entity whose delta was never written goes without
    /// state; its full state comes with its next delta.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// An entity has spawned since the last send: it has no state yet, and the next packet would spawn it again.
    /// </exception>
    /// <exception cref="ReplicationException">A manual member cannot be written.</exception>
    public void WriteSnapshot(BitWriter writer) => WriteAll(writer, saving: false);

    /// <summary>
    /// The records of <see cref="WriteSnapshot"/>, but a <see cref="NotSavedAttribute"/> entity goes without state.
    /// Between ticks too: an entity that has left the tree since the send is still written, from its baseline.
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
            throw new InvalidOperationException(StateLostError.FormatWith(string.Join(", ", lost)));
        }
        WriteAll(writer, saving: true);
    }

    private void WriteAll(BitWriter writer, bool saving)
    {
        if (_spawned.Count > 0)
        {
            throw new InvalidOperationException(SpawnedSinceSendError.FormatWith(_spawned.Count));
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
            // No delta for a kind with no replicated member, nor for a first delta that threw: RepliCAT has reset the
            // baseline, so the models section of the next packet carries the full state
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
            // RepliCAT has reset the baseline, so the next delta of this entity is complete
            _log.Error(e, DeltaFailedLog, id);
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

    // Spawned and despawned before a send: no client has heard of it
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
