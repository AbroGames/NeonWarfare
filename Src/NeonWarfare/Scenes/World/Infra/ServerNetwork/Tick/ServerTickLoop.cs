using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Replication;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;

/// <summary>
/// One server tick: the commands received since the last one, then the time rules of the facades, then sending.
/// Everything the Simulation published during the tick leaves at its end, the host's own peer included. The state
/// packet goes first, so the events of the tick are handled on a client whose models are already at its end.
/// A peer that joined in the tick gets the world snapshot instead: it is written after the state packet, from the
/// baselines that packet has just brought to the end of the tick. A save requested in the tick is written from the
/// same baselines, after the snapshots.
/// </summary>
[ServerNetwork]
public class ServerTickLoop(
    CommandDispatcher commands,
    PeerGatekeeper gatekeeper,
    EventOutbox outbox,
    StateReplicator stateReplicator,
    SaveWriter saveWriter,
    IClientsConnection clientsConnection)
{
    private const string RestoreAfterTickError = "The tick counter is {0}: a restore comes before the first tick.";
    private const string NegativeTickError = "Tick {0} is negative.";
    private const string SendFailedLog = "Events packet for peer {peerId} failed, the other peers still get theirs";
    private const string StateSendFailedLog =
        "State packet for peer {peerId} failed, disconnecting it, the other peers still get theirs";
    private const string SnapshotWriteFailedLog =
        "The snapshot failed, rejecting the peers joined in the tick: {peerIds}";
    private const string SnapshotSendFailedLog =
        "Snapshot for peer {peerId} failed, disconnecting it, the other peers still get theirs";
    private const string RejectFailedLog = "Rejecting peer {peerId} failed, it is disconnected anyway";

    private readonly ILogger _log = LogFactory.GetForStatic<ServerTickLoop>();

    private bool _started;

    /// <summary>
    /// Grows at the start of <see cref="RunTick"/>: inside tick N it is N, and it stays N until tick N + 1 starts.
    /// 0 before the first tick of a new world, the saved tick before the first tick of a loaded one.
    /// </summary>
    public long CurrentTick { get; private set; }

    /// <summary>
    /// Continues the tick counter of a loaded world: the first tick after it is <paramref name="tick"/> + 1.
    /// </summary>
    public void Restore(long tick)
    {
        if (_started) throw new InvalidOperationException(RestoreAfterTickError.FormatWith(CurrentTick));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), tick, NegativeTickError.FormatWith(tick));

        CurrentTick = tick;
    }

    // Calls from ServerTickNode
    public void RunTick()
    {
        _started = true;
        CurrentTick++;
        
        // A peer that joins in this tick gets the state at its end from its snapshot, and has nothing to apply a
        // delta to before that
        List<int> joinedBefore = outbox.Peers.ToList();
        
        commands.ProcessAll();
        gatekeeper.DisconnectExpired();
        //TODO Tick() of the facades with a time rule, once the first one appears
        SendState(joinedBefore);
        SendSnapshots(joinedBefore);
        saveWriter.WriteRequested();
        SendEvents();
    }

    // A disconnected peer keeps its buffer and binding until its peer_disconnected, so that its Leave still runs,
    // but has nothing more to receive: a rejected joiner has no world to apply a packet to
    private bool IsReachable(int peerId) => outbox.Peers.Contains(peerId) && !gatekeeper.IsDisconnecting(peerId);

    private void SendState(List<int> joinedBefore)
    {
        var writer = new BitWriter();
        if (!stateReplicator.TryWrite(CurrentTick, writer)) return;

        ReadOnlySpan<byte> packet = writer.AsSpan();
        // The host's own World has the state already: its Simulation wrote it
        foreach (int peerId in joinedBefore.Where(peerId => peerId != clientsConnection.LocalPeerId))
        {
            if (!IsReachable(peerId)) continue;

            try
            {
                clientsConnection.Send(peerId, packet);
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
        List<int> joined = outbox.Peers
            .Except(joinedBefore)
            .Where(peerId => peerId != clientsConnection.LocalPeerId && IsReachable(peerId))
            .ToList();
        if (joined.Count == 0) return;

        var writer = new BitWriter();
        try
        {
            writer.WriteBits((byte) ServerPacketKind.Snapshot, 8);
            writer.WriteVarUInt((ulong) CurrentTick);
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

        ReadOnlySpan<byte> packet = writer.AsSpan();
        foreach (int peerId in joined)
        {
            try
            {
                clientsConnection.Send(peerId, packet);
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
        ArrayBufferWriter<byte> packet = new();
        foreach (int peerId in outbox.Peers.Where(IsReachable).ToList())
        {
            // On the host this also covers its own World throwing: Game delivers the loopback inside Send
            try
            {
                packet.ResetWrittenCount();
                packet.GetSpan(1)[0] = (byte) ServerPacketKind.Events;
                packet.Advance(1);
                if (outbox.DrainEvents(peerId, packet) == 0) continue;

                clientsConnection.Send(peerId, packet.WrittenSpan);
            }
            catch (Exception e)
            {
                _log.Error(e, SendFailedLog, peerId);
            }
        }
    }
}
