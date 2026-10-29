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

    private record Tracked(Node Node, ReplicationBaseline Baseline);

    private readonly ILogger _log = LogFactory.GetForStatic<StateReplicator>();

    private readonly IEntityFinder _entities;
    private readonly Replicator _replicator;
    // By NetId: the order is the same on every run, so the packet is too
    private readonly SortedDictionary<long, Tracked> _trackedById = new();

    public StateReplicator(IEntityFinder entities, Replicator replicator)
    {
        _entities = entities;
        _replicator = replicator;
        _entities.SpawnedEvent += OnEntitySpawned;
    }

    /// <summary>
    /// Called every tick, whether anyone gets the packet or not: the baselines stay equal to the state at the end of
    /// the last tick, which is what a joining peer's snapshot is built from.
    /// </summary>
    /// <returns><c>false</c> when no model changed: the writer is left as it was, and nothing is sent.</returns>
    public bool TryWrite(long tick, BitWriter writer)
    {
        int start = writer.BitPosition;
        writer.WriteBits((byte) ServerPacketKind.State, 8);
        writer.WriteVarUInt((ulong) tick);

        bool written = false;
        List<long> gone = null;
        foreach ((long id, Tracked tracked) in _trackedById)
        {
            if (!_entities.TryGetNode(new NetId(id), out Node node) || node != tracked.Node)
            {
                (gone ??= []).Add(id);
                continue;
            }

            int recordStart = writer.BitPosition;
            writer.WriteVarUInt((ulong) id);
            try
            {
                if (_replicator.TryWriteDelta(tracked.Baseline, writer))
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
        gone?.ForEach(id => _trackedById.Remove(id));

        if (!written)
        {
            writer.Rewind(start);
            return false;
        }

        writer.WriteVarUInt((ulong) NetId.None.Value);
        return true;
    }

    private void OnEntitySpawned(NetId id, Node node) =>
        _trackedById.Add(id.Value, new Tracked(node, _replicator.CreateBaseline(node)));
}
