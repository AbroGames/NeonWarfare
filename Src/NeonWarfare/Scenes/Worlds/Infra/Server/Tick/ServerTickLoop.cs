using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Infra.Server.Replication;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Ports;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

/// <summary>
/// One server tick. The order of its steps is in Docs/Networking.md#the-server-tick.
/// </summary>
[Server]
public class ServerTickLoop(
    ServerTickClock clock,
    CommandDispatcher commands,
    PeerGatekeeper gatekeeper,
    PeerStateTable peers,
    EventOutbox outbox,
    StateReplicator stateReplicator,
    SaveService saveService,
    IClientsConnection clientsConnection)
{
    private readonly ILogger _log = LogFactory.GetForStatic<ServerTickLoop>();

    public void RunTick()
    {
        clock.StartTick();
        
        // A peer that joins in this tick gets its snapshot instead of the state packet.
        // It has nothing yet to apply a delta to.
        List<int> joinedBefore = peers.JoinedPeerIds.ToList();
        
        commands.ProcessAll();
        gatekeeper.DisconnectExpired();
        // TODO Call Tick() of the facades with a time rule once the first one appears.
        SendState(joinedBefore);
        SendSnapshots(joinedBefore);
        saveService.WriteRequested();
        SendEvents();
    }

    private void SendState(List<int> joinedBefore)
    {
        var writer = new BitWriter();
        if (!stateReplicator.TryWrite(clock.CurrentTick, writer)) return;

        ReadOnlySpan<byte> body = writer.AsSpan();
        // The host's own World already has the state, because its Simulation wrote it.
        foreach (int peerId in joinedBefore.Where(peerId => peerId != clientsConnection.LocalPeerId))
        {
            // A peer disconnected earlier in the tick is only waiting for its peer_disconnected.
            if (!peers.IsJoined(peerId)) continue;

            // The baselines have moved past what the peer has, so every next delta would corrupt its models.
            // The peer is disconnected and comes back through a join with a fresh snapshot.
            if (!clientsConnection.SendState(peerId, body)) gatekeeper.Disconnect(peerId);
        }
    }

    // The host's own peer gets no snapshot, because its World is the server's.
    private void SendSnapshots(List<int> joinedBefore)
    {
        List<int> joined = peers.JoinedPeerIds
            .Except(joinedBefore)
            .Where(peerId => peerId != clientsConnection.LocalPeerId)
            .ToList();
        if (joined.Count == 0) return;

        var writer = new BitWriter();
        try
        {
            writer.WriteVarUInt((ulong) clock.CurrentTick);
            stateReplicator.WriteSnapshot(writer);
        }
        // A peer without a consistent snapshot would silently drift from the next deltas on, so it is rejected.
        catch (Exception e)
        {
            _log.Error(e, "The snapshot failed, rejecting the peers joined in the tick: {peerIds}", joined);
            joined.ForEach(peerId => gatekeeper.Reject(peerId, JoinRejectReason.InternalError));
            return;
        }

        ReadOnlySpan<byte> body = writer.AsSpan();
        foreach (int peerId in joined)
        {
            if (!clientsConnection.SendSnapshot(peerId, body)) gatekeeper.Disconnect(peerId);
        }
    }

    private void SendEvents()
    {
        ArrayBufferWriter<byte> section = new();
        foreach (int peerId in peers.JoinedPeerIds.ToList())
        {
            section.ResetWrittenCount();
            if (outbox.DrainEvents(peerId, section) == 0) continue;

            // The peer's models are fine, but it has lost the events of the tick, so it comes back through a join.
            if (!clientsConnection.SendEvents(peerId, section.WrittenSpan)) gatekeeper.Disconnect(peerId);
        }
    }
}
