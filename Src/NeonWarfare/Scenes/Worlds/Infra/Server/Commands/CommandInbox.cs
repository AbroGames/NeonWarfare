using System;
using System.Collections.Generic;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

/// <summary>
/// The commands received since the last tick, in arrival order, together with the disconnections: a peer's
/// commands and its leaving share one order. A packet is decoded on arrival, so a broken one is reported at once
/// and never reaches the queue; whatever depends on the world state is left to <see cref="CommandDispatcher"/>.
/// </summary>
[Server]
public class CommandInbox(
    NetMessageCodec codec, ProtocolHash protocolHash, PeerGatekeeper gatekeeper, CommandHandlerRegistry handlers)
{
    public abstract record Entry(int PeerId);
    public record PeerCommandEntry(int PeerId, Command Command) : Entry(PeerId);
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
            _log.Warning("Packet from peer {peerId} dropped: {reason}", peerId, e.Message);
            return;
        }

        if (bytesRead != packet.Length)
        {
            _log.Warning("Packet from peer {peerId} dropped: {bytesRead} of {length} bytes read, a packet carries one "
                + "command", peerId, bytesRead, packet.Length);
            return;
        }

        // A protocol check, not a game rule, so it is not the session handler's: a client of another build is rejected
        // before its join can reach the world
        if (command is JoinRequestCommand join && join.ProtocolHash != protocolHash.Value)
        {
            _log.Warning("JoinRequestCommand from peer {peerId} has protocol hash {theirs}, the server has {ours}",
                peerId, join.ProtocolHash, protocolHash.Value);
            gatekeeper.Reject(peerId, JoinRejectReason.ProtocolMismatch);
            return;
        }

        _entries.Add(new PeerCommandEntry(peerId, command));
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
