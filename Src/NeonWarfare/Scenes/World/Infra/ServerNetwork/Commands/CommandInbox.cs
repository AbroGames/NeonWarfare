using System;
using System.Collections.Generic;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;

/// <summary>
/// The commands received since the last tick, in arrival order, together with the disconnections: a peer's
/// commands and its leaving share one order. A packet is decoded on arrival, so a broken one is reported at once
/// and never reaches the queue; whatever depends on the world state is left to <see cref="CommandDispatcher"/>.
/// </summary>
[ServerNetwork]
public class CommandInbox(NetMessageCodec codec, PeerGatekeeper gatekeeper, CommandHandlerRegistry handlers)
{
    private const string RejectedPacketLog = "Packet from peer {peerId} dropped: {reason}";
    private const string TrailingBytesReason = "{0} of {1} bytes read, a packet carries one command";
    private const string ProtocolMismatchLog =
        "JoinRequestCommand from peer {peerId} has protocol hash {theirs}, the server has {ours}";

    public abstract record Entry(int PeerId);
    public record PeerCommand(int PeerId, Command Command) : Entry(PeerId);
    // Not a Command, so it never enters the network whitelist: a peer cannot send its own disconnection
    public record PeerDisconnected(int PeerId) : Entry(PeerId);

    private readonly ILogger _log = LogFactory.GetForStatic<CommandInbox>();

    private List<Entry> _entries = [];

    public void EnqueueFromPeer(int peerId, ReadOnlyMemory<byte> packet)
    {
        Command command;
        int bytesRead;
        try
        {
            command = (Command) codec.Read(packet, handlers.NetworkCommandTypes, out bytesRead);
        }
        catch (NetMessageFormatException e)
        {
            _log.Warning(RejectedPacketLog, peerId, e.Message);
            return;
        }

        if (bytesRead != packet.Length)
        {
            _log.Warning(RejectedPacketLog, peerId, TrailingBytesReason.FormatWith(bytesRead, packet.Length));
            return;
        }

        // A protocol check, not a game rule, so it is not the join handler's: a client of another build is rejected
        // before its join can reach the world
        if (command is JoinRequestCommand join && join.ProtocolHash != codec.ProtocolHash)
        {
            _log.Warning(ProtocolMismatchLog, peerId, join.ProtocolHash, codec.ProtocolHash);
            gatekeeper.Reject(peerId, JoinRejectReason.ProtocolMismatch);
            return;
        }

        _entries.Add(new PeerCommand(peerId, command));
    }

    public void EnqueuePeerDisconnected(int peerId) => _entries.Add(new PeerDisconnected(peerId));

    // A new list rather than Clear: whatever is enqueued while these are processed waits for the next tick
    public IReadOnlyList<Entry> TakeAll()
    {
        List<Entry> taken = _entries;
        _entries = [];
        return taken;
    }
}
