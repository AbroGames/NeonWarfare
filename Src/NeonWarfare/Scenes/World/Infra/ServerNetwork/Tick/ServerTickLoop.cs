using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Replication;
using RepliCAT.Bits;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;

/// <summary>
/// One server tick: the commands received since the last one, then the time rules of the facades, then sending.
/// Everything the Simulation published during the tick leaves at its end, the host's own peer included. The state
/// packet goes first, so the events of the tick are handled on a client whose models are already at its end.
/// </summary>
[ServerNetwork]
public class ServerTickLoop(
    CommandDispatcher commands,
    PeerGatekeeper gatekeeper,
    EventOutbox outbox,
    StateReplicator stateReplicator,
    IClientsConnection clientsConnection)
{
    private const string SendFailedLog = "Events packet for peer {peerId} failed, the other peers still get theirs";
    private const string StateSendFailedLog =
        "State packet for peer {peerId} failed, disconnecting it, the other peers still get theirs";

    private readonly ILogger _log = LogFactory.GetForStatic<ServerTickLoop>();

    /// <summary>
    /// Grows at the start of <see cref="RunTick"/>: inside tick N it is N, and it stays N until tick N + 1 starts.
    /// 0 before the first tick.
    /// </summary>
    public long CurrentTick { get; private set; }

    // Calls from ServerTickNode
    public void RunTick()
    {
        CurrentTick++;
        
        // A peer that joins in this tick gets the state at its end from its snapshot, and has nothing to apply a
        // delta to before that
        List<int> joinedBefore = outbox.Peers.ToList();
        
        commands.ProcessAll();
        gatekeeper.DisconnectExpired();
        //TODO Tick() of the facades with a time rule, once the first one appears
        //TODO 021a/022a deferred join and save snapshots
        SendState(joinedBefore);
        SendEvents();
    }

    private void SendState(List<int> joinedBefore)
    {
        var writer = new BitWriter();
        if (!stateReplicator.TryWrite(CurrentTick, writer)) return;

        ReadOnlySpan<byte> packet = writer.AsSpan();
        // The host's own World has the state already: its Simulation wrote it
        foreach (int peerId in joinedBefore.Where(peerId => peerId != clientsConnection.LocalPeerId))
        {
            if (!outbox.Peers.Contains(peerId)) continue;

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

    private void SendEvents()
    {
        ArrayBufferWriter<byte> packet = new();
        foreach (int peerId in outbox.Peers.ToList())
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
