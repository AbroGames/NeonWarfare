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
/// One server tick: the commands received since the last one, then the time rules of the facades, then sending.
/// Everything the Simulation published during the tick leaves at its end, the host's own peer included. The state
/// packet goes first, so the events of the tick are handled on a client whose models are already at its end.
/// A peer that joined in the tick gets the world snapshot instead: it is written after the state packet, from the
/// baselines that packet has just brought to the end of the tick. A save requested in the tick is written from the
/// same baselines, after the snapshots.
/// </summary>
[Server]
public class ServerTickLoop(
    ServerTickClock clock,
    CommandDispatcher commands,
    PeerGatekeeper gatekeeper,
    PeerStateTable peers,
    EventOutbox outbox,
    StateReplicator stateReplicator,
    SaveWriter saveWriter,
    IClientsConnection clientsConnection)
{
    private const string SendFailedLog = "Events packet for peer {peerId} failed, the other peers still get theirs";
    private const string StateSendFailedLog =
        "State packet for peer {peerId} failed, disconnecting it, the other peers still get theirs";
    private const string SnapshotWriteFailedLog =
        "The snapshot failed, rejecting the peers joined in the tick: {peerIds}";
    private const string SnapshotSendFailedLog =
        "Snapshot for peer {peerId} failed, disconnecting it, the other peers still get theirs";
    private const string RejectFailedLog = "Rejecting peer {peerId} failed, it is disconnected anyway";

    private readonly ILogger _log = LogFactory.GetForStatic<ServerTickLoop>();

    // Calls from ServerTickNode
    public void RunTick()
    {
        clock.StartTick();
        
        // A peer that joins in this tick gets the state at its end from its snapshot, and has nothing to apply a
        // delta to before that
        List<int> joinedBefore = peers.JoinedPeerIds.ToList();
        
        commands.ProcessAll();
        gatekeeper.DisconnectExpired();
        //TODO Tick() of the facades with a time rule, once the first one appears
        SendState(joinedBefore);
        SendSnapshots(joinedBefore);
        saveWriter.WriteRequested();
        SendEvents();
    }

    private void SendState(List<int> joinedBefore)
    {
        var writer = new BitWriter();
        if (!stateReplicator.TryWrite(clock.CurrentTick, writer)) return;

        ReadOnlySpan<byte> body = writer.AsSpan();
        // The host's own World has the state already: its Simulation wrote it
        foreach (int peerId in joinedBefore.Where(peerId => peerId != clientsConnection.LocalPeerId))
        {
            // Disconnected earlier in the tick, it is only waiting for its peer_disconnected
            if (!peers.IsJoined(peerId)) continue;

            try
            {
                clientsConnection.SendState(peerId, body);
            }
            catch (Exception e)
            {
                _log.Error(e, StateSendFailedLog, peerId);
                // The baselines have moved past what the peer has, so every next delta would corrupt its models;
                // it comes back through a join and gets a fresh snapshot
                gatekeeper.Disconnect(peerId);
            }
        }
    }

    // The host's own World is the server's: it has every entity already
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
        catch (Exception e)
        {
            // Without a consistent snapshot the peer's models would silently drift from the next deltas on
            _log.Error(e, SnapshotWriteFailedLog, joined);
            foreach (int peerId in joined)
            {
                // A failed rejection has disconnected the peer anyway, and the rest still have to be rejected
                try
                {
                    gatekeeper.Reject(peerId, JoinRejectReason.InternalError);
                }
                catch (Exception rejectError)
                {
                    _log.Error(rejectError, RejectFailedLog, peerId);
                }
            }
            return;
        }

        ReadOnlySpan<byte> body = writer.AsSpan();
        foreach (int peerId in joined)
        {
            try
            {
                clientsConnection.SendSnapshot(peerId, body);
            }
            catch (Exception e)
            {
                _log.Error(e, SnapshotSendFailedLog, peerId);
                gatekeeper.Disconnect(peerId);
            }
        }
    }

    private void SendEvents()
    {
        ArrayBufferWriter<byte> section = new();
        foreach (int peerId in peers.JoinedPeerIds.ToList())
        {
            // On the host this also covers its own World throwing: its transport delivers the events inside the call
            try
            {
                section.ResetWrittenCount();
                if (outbox.DrainEvents(peerId, section) == 0) continue;

                clientsConnection.SendEvents(peerId, section.WrittenSpan);
            }
            catch (Exception e)
            {
                _log.Error(e, SendFailedLog, peerId);
            }
        }
    }
}
