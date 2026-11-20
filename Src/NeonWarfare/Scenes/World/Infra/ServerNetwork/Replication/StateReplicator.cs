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
/// Writes the state packet of the tick: the kind byte, the tick number (varuint), then the models section — for every
/// entity whose models changed, its NetId (varuint) and its RepliCAT delta, closed by <see cref="NetId.None"/>. A delta
/// carries no length, so the client must know every NetId in the packet. One packet for every peer: a written delta
/// has already moved its baseline.
/// </summary>
[ServerNetwork]
public class StateReplicator
{
    private const string DeltaFailedLog = "The delta of {netId} failed, the other entities are still written";

    private readonly ILogger _log = LogFactory.GetForStatic<StateReplicator>();

    private readonly Replicator _replicator;
    // By NetId: the order is the same on every run, so the packet is too
    private readonly SortedDictionary<long, ReplicationBaseline> _baselineById = new();
    // Their baselines stay until the end of the tick: the set of baselines is the set of entities at the last send
    private readonly List<NetId> _despawned = [];

    public StateReplicator(IEntityFinder entities, Replicator replicator)
    {
        _replicator = replicator;
        entities.SpawnedEvent += OnEntitySpawned;
        entities.DespawnedEvent += OnEntityDespawned;
    }

    /// <summary>
    /// Called every tick, whether anyone gets the packet or not: the baselines stay equal to the state at the end of
    /// the last tick, which is what a joining peer's snapshot is built from.
    /// </summary>
    /// <returns><c>false</c> when no model changed: the writer is left as it was, and nothing is sent.</returns>
    public bool TryWrite(long tick, BitWriter writer)
    {
        _despawned.ForEach(id => _baselineById.Remove(id.Value));
        _despawned.Clear();

        int start = writer.BitPosition;
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt((ulong) tick);

        bool written = false;
        foreach ((long id, ReplicationBaseline baseline) in _baselineById)
        {
            int recordStart = writer.BitPosition;
            writer.WriteVarUInt((ulong) id);
            try
            {
                if (_replicator.TryWriteDelta(baseline, writer))
                {
                    written = true;
                    continue;
                }
            }
            catch (ReplicationException e)
            {
                // RepliCAT has reset the baseline, so the next delta of this entity is complete
                _log.Error(e, DeltaFailedLog, new NetId(id));
            }
            writer.Rewind(recordStart);
        }

        if (!written)
        {
            writer.Rewind(start);
            return false;
        }

        writer.WriteVarUInt((ulong) NetId.None.Value);
        return true;
    }

    private void OnEntitySpawned(NetId id, Node node) => _baselineById.Add(id.Value, _replicator.CreateBaseline(node));

    private void OnEntityDespawned(NetId id, Node node) => _despawned.Add(id);
}
