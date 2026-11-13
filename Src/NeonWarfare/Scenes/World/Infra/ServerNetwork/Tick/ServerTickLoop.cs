using System;
using System.Buffers;
using System.Linq;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;

/// <summary>
/// One server tick: the commands received since the last one, then the time rules of the facades, then sending.
/// Everything the Simulation published during the tick leaves at its end, the host's own peer included.
/// </summary>
[ServerNetwork]
public class ServerTickLoop(
    CommandDispatcher commands,
    PeerGatekeeper gatekeeper,
    EventOutbox outbox,
    IClientsConnection clientsConnection)
{
    private const string SendFailedLog = "Events packet for peer {peerId} failed, the other peers still get theirs";

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
        commands.ProcessAll();
        gatekeeper.DisconnectExpired();
        //TODO Tick() of the facades with a time rule, once the first one appears
        //TODO 021/022 deferred join and save snapshots
        SendEvents();
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
