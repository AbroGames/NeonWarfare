using System;
using System.Buffers;
using System.Collections.Generic;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.World.ServerNetwork;

/// <summary>
/// Events from the Simulation to the clients, sent at the end of the tick. Not a bus: nothing on the server
/// subscribes to it. One buffer per joined peer keeps personal and common events in the order of publication;
/// the console of a dedicated server with <c>ServerHud</c> is an addressee of its own that gets common and console
/// events, never the personal events of players.
/// </summary>
[ServerNetwork]
public class EventOutbox(NetMessageCodec codec, PeerUidMap peers)
{
    private const string OfflineReceiverLog = "{event} for player {uid} dropped: the player is not on any peer";
    private const string NoConsoleLog = "{event} for the console dropped: this world has no console";
    private const string NoConsoleError = "This world has no console.";
    private const string NoPeerBufferError = "Player {0} is bound to peer {1}, which has no event buffer.";
    private const string PeerExistsError = "Peer {0} already has an event buffer.";
    private const string PeerMissingError = "Peer {0} has no event buffer.";
    private const string ConsoleExistsError = "The console already has an event buffer.";

    private readonly ILogger _log = LogFactory.GetForStatic<EventOutbox>();

    private readonly Dictionary<int, List<ReadOnlyMemory<byte>>> _bufferByPeerId = new();
    private List<ReadOnlyMemory<byte>> _consoleBuffer;

    public IReadOnlyCollection<int> Peers => _bufferByPeerId.Keys;
    public bool HasConsole => _consoleBuffer != null;

    // Encoded once, here: every addressee gets the same bytes, and an event that cannot be serialized fails in
    // the Simulation that published it rather than at the end of the tick
    public void PublishToAll(object @event)
    {
        byte[] encoded = codec.Encode(@event);
        foreach (List<ReadOnlyMemory<byte>> buffer in _bufferByPeerId.Values)
        {
            buffer.Add(encoded);
        }
        _consoleBuffer?.Add(encoded);
    }

    // A receiver is always online: callers pick receivers through PlayerQuery, and a command's sender cannot have
    // left yet because commands and disconnections share one ordered queue. Breaking that is logged and dropped
    // rather than thrown, so the rest of the tick still goes out.
    public void PublishTo(object @event, PlayerModel receiver)
    {
        byte[] encoded = codec.Encode(@event);
        if (!peers.TryGetPeerId(receiver.Uid, out int peerId))
        {
            _log.Error(OfflineReceiverLog, @event.GetType().Name, receiver.Uid);
            return;
        }
        if (!_bufferByPeerId.TryGetValue(peerId, out List<ReadOnlyMemory<byte>> buffer))
        {
            throw new InvalidOperationException(NoPeerBufferError.FormatWith(receiver.Uid, peerId));
        }

        buffer.Add(encoded);
    }

    // Only console commands reply to the console, and their input exists only with ServerHud
    public void PublishToConsole(object @event)
    {
        byte[] encoded = codec.Encode(@event);
        if (_consoleBuffer == null)
        {
            _log.Error(NoConsoleLog, @event.GetType().Name);
            return;
        }

        _consoleBuffer.Add(encoded);
    }

    public void AddPeer(int peerId)
    {
        if (!_bufferByPeerId.TryAdd(peerId, []))
        {
            throw new InvalidOperationException(PeerExistsError.FormatWith(peerId));
        }
    }

    public void RemovePeer(int peerId)
    {
        if (!_bufferByPeerId.Remove(peerId))
        {
            throw new InvalidOperationException(PeerMissingError.FormatWith(peerId));
        }
    }

    public void AddConsole()
    {
        if (_consoleBuffer != null)
        {
            throw new InvalidOperationException(ConsoleExistsError);
        }

        _consoleBuffer = [];
    }

    /// <summary>
    /// Writes the events section of the peer's packet and empties its buffer. An empty section is written too:
    /// the packet may still carry spawns or models.
    /// </summary>
    /// <returns>The number of events written, so that an empty packet can be skipped.</returns>
    public int DrainPeerEvents(int peerId, IBufferWriter<byte> output)
    {
        if (!_bufferByPeerId.TryGetValue(peerId, out List<ReadOnlyMemory<byte>> buffer))
        {
            throw new InvalidOperationException(PeerMissingError.FormatWith(peerId));
        }

        return Drain(buffer, output);
    }

    /// <inheritdoc cref="DrainPeerEvents"/>
    public int DrainConsoleEvents(IBufferWriter<byte> output)
    {
        if (_consoleBuffer == null)
        {
            throw new InvalidOperationException(NoConsoleError);
        }

        return Drain(_consoleBuffer, output);
    }

    private int Drain(List<ReadOnlyMemory<byte>> buffer, IBufferWriter<byte> output)
    {
        codec.WriteSection(output, buffer);
        int count = buffer.Count;
        buffer.Clear();
        return count;
    }
}
