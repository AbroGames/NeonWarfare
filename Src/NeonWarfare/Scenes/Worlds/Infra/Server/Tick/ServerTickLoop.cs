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
    IClientsConnection clientsConnection,
    IEnumerable<ITickingFacade> tickingFacades)
{
    private readonly ILogger _log = LogFactory.GetForStatic<ServerTickLoop>();

    public void RunTick()
    {
        clock.StartTick();
        
        // A peer that joins in this tick gets its snapshot instead of the state section.
        // It has nothing yet to apply a delta to.
        HashSet<int> joinedBefore = peers.JoinedPeerIds.ToHashSet();
        
        commands.ProcessAll();
        gatekeeper.DisconnectExpired();
        TickFacades();
        var state = new BitWriter();
        bool stateWritten = stateReplicator.TryWrite(state);
        try
        {
            SendSnapshots(joinedBefore);
            saveService.WriteRequested();
        }
        // Logged here, because an exception from the sends below would replace this one.
        catch (Exception e)
        {
            _log.Error(e, "A tick step after the state write failed, the tick updates are still sent");
            throw;
        }
        // The baselines have already moved, so the remote peers must get the state section even if a step throws.
        // Otherwise their models would drift silently.
        finally
        {
            SendTickUpdates(joinedBefore, stateWritten ? state.AsSpan() : ReadOnlySpan<byte>.Empty);
        }
    }

    // Each facade is its own fault boundary, because one that throws would throw every tick and stop every packet.
    private void TickFacades()
    {
        foreach (ITickingFacade facade in tickingFacades)
        {
            try
            {
                facade.Tick();
            }
            catch (Exception e)
            {
                _log.Error(e, "The tick of {facade} failed, the other facades still tick", facade.GetType().Name);
            }
        }
    }

    // The host's own peer gets no snapshot, because its World is the server's.
    private void SendSnapshots(HashSet<int> joinedBefore)
    {
        List<int> joined = peers.JoinedPeerIds
            .Except(joinedBefore)
            .Where(peerId => peerId != clientsConnection.LocalPeerId)
            .ToList();
        if (joined.Count == 0) return;

        var writer = new BitWriter();
        try
        {
            stateReplicator.WriteSnapshot(writer);
        }
        // A peer without a consistent snapshot would silently drift from the next deltas on, so it is rejected.
        catch (Exception e)
        {
            _log.Error(e, "The snapshot failed, rejecting the peers joined in the tick: {peerIds}", joined);
            joined.ForEach(peerId => gatekeeper.Reject(peerId, JoinRejectReason.InternalError));
            return;
        }

        ReadOnlySpan<byte> records = writer.AsSpan();
        foreach (int peerId in joined)
        {
            if (!clientsConnection.SendSnapshot(peerId, clock.CurrentTick, records)) gatekeeper.Disconnect(peerId);
        }
    }

    private void SendTickUpdates(HashSet<int> joinedBefore, ReadOnlySpan<byte> state)
    {
        ArrayBufferWriter<byte> events = new();
        // A peer disconnected earlier in the tick is no longer joined and gets nothing.
        foreach (int peerId in peers.JoinedPeerIds.ToList())
        {
            events.ResetWrittenCount();
            ReadOnlySpan<byte> peerEvents =
                outbox.DrainEvents(peerId, events) > 0 ? events.WrittenSpan : ReadOnlySpan<byte>.Empty;
            // The host's own World already has the state, because its Simulation wrote it.
            ReadOnlySpan<byte> peerState = joinedBefore.Contains(peerId) && peerId != clientsConnection.LocalPeerId
                ? state
                : ReadOnlySpan<byte>.Empty;
            if (peerState.IsEmpty && peerEvents.IsEmpty) continue;

            // The peer has missed a reliable packet, so it comes back through a join with a fresh snapshot.
            if (!clientsConnection.SendTickUpdate(peerId, clock.CurrentTick, peerState, peerEvents))
            {
                gatekeeper.Disconnect(peerId);
            }
        }
    }
}
