using System;
using System.Collections.Generic;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.World.ServerNetwork;

/// <summary>
/// The commands received since the last tick, in arrival order, together with the disconnections: a peer's
/// commands and its leaving share one order. A packet is decoded on arrival, so a broken one is reported at once
/// and never reaches the queue; whatever depends on the world state is left to <see cref="CommandDispatcher"/>.
/// </summary>
[ServerNetwork]
public class CommandInbox(NetMessageCodec codec)
{
    private const string RejectedPacketLog = "Packet from peer {peerId} dropped: {reason}";
    private const string TrailingBytesReason = "{0} of {1} bytes read, a packet carries one command";
    private const string RegisteredError = "The network command types are already registered.";
    private const string NotRegisteredError = "The network command types are not registered yet.";

    public abstract record Entry(int PeerId);
    public record PeerCommand(int PeerId, Command Command) : Entry(PeerId);
    // Not a Command, so it never enters the network whitelist: a peer cannot send its own disconnection
    public record PeerDisconnected(int PeerId) : Entry(PeerId);

    private readonly ILogger _log = LogFactory.GetForStatic<CommandInbox>();

    private List<Entry> _entries = [];
    private IReadOnlySet<Type> _networkCommandTypes;

    // From the composition root rather than the constructor: the set is built by the dispatcher from its handlers
    public void Register(IReadOnlySet<Type> networkCommandTypes)
    {
        if (_networkCommandTypes != null)
        {
            throw new InvalidOperationException(RegisteredError);
        }

        _networkCommandTypes = networkCommandTypes;
    }

    public void EnqueueFromPeer(int peerId, ReadOnlyMemory<byte> packet)
    {
        if (_networkCommandTypes == null)
        {
            throw new InvalidOperationException(NotRegisteredError);
        }

        Command command;
        int bytesRead;
        try
        {
            command = (Command) codec.Read(packet, _networkCommandTypes, out bytesRead);
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
