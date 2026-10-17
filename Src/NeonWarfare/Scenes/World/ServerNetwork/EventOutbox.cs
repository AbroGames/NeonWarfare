using System;
using System.Buffers;
using System.Collections.Generic;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.World.ServerNetwork;

/// <summary>
/// Events from the Simulation to the clients, sent at the end of the tick. Not a bus: nothing on the server
/// subscribes to it. One buffer per joined peer keeps personal and common events in the order of publication;
/// the window of a dedicated server with <c>ServerHud</c> is an addressee of its own that gets common and
/// dedicated-window events, never the personal events of players.
/// </summary>
[ServerNetwork]
public class EventOutbox(NetMessageCodec codec, PeerUidMap peers)
{
    private const string OfflineReceiverLog = "{event} for player {uid} dropped: the player is not on any peer";
    private const string NoDedicatedWindowLog =
        "{event} for the dedicated window dropped: this world has no dedicated window";
    private const string NoDedicatedWindowError = "This world has no dedicated window.";
    private const string NoPeerBufferError = "Player {0} is bound to peer {1}, which has no event buffer.";
    private const string PeerExistsError = "Peer {0} already has an event buffer.";
    private const string PeerMissingError = "Peer {0} has no event buffer.";
    private const string DedicatedWindowExistsError = "The dedicated window already has an event buffer.";

    private readonly ILogger _log = LogFactory.GetForStatic<EventOutbox>();

    private readonly Dictionary<int, List<ReadOnlyMemory<byte>>> _bufferByPeerId = new();
    private List<ReadOnlyMemory<byte>> _dedicatedWindowBuffer;

    public IReadOnlyCollection<int> Peers => _bufferByPeerId.Keys;
    public bool HasDedicatedWindow => _dedicatedWindowBuffer != null;

    // Encoded once, here: every addressee gets the same bytes, and an event that cannot be serialized fails in
    // the Simulation that published it rather than at the end of the tick
    public void PublishToAll(Event @event)
    {
        byte[] encoded = codec.Encode(@event);
        foreach (List<ReadOnlyMemory<byte>> buffer in _bufferByPeerId.Values)
        {
            buffer.Add(encoded);
        }
        _dedicatedWindowBuffer?.Add(encoded);
    }

    // A receiver is always online: callers pick receivers through PlayerQuery, and a command's sender cannot have
    // left yet because commands and disconnections share one ordered queue. Breaking that is logged and dropped
    // rather than thrown, so the rest of the tick still goes out.
    public void PublishTo(Event @event, PlayerModel receiver)
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

    // Only dedicated-window commands reply to the window, and their input exists only with ServerHud
    public void PublishToDedicatedWindow(Event @event)
    {
        byte[] encoded = codec.Encode(@event);
        if (_dedicatedWindowBuffer == null)
        {
            _log.Error(NoDedicatedWindowLog, @event.GetType().Name);
            return;
        }

        _dedicatedWindowBuffer.Add(encoded);
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

    public void AddDedicatedWindow()
    {
        if (_dedicatedWindowBuffer != null)
        {
            throw new InvalidOperationException(DedicatedWindowExistsError);
        }

        _dedicatedWindowBuffer = [];
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
    public int DrainDedicatedWindowEvents(IBufferWriter<byte> output)
    {
        if (_dedicatedWindowBuffer == null)
        {
            throw new InvalidOperationException(NoDedicatedWindowError);
        }

        return Drain(_dedicatedWindowBuffer, output);
    }

    private int Drain(List<ReadOnlyMemory<byte>> buffer, IBufferWriter<byte> output)
    {
        codec.WriteSection(output, buffer);
        int count = buffer.Count;
        buffer.Clear();
        return count;
    }
}
