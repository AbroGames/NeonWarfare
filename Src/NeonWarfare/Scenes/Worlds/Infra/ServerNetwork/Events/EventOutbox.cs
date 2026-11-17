using System;
using System.Buffers;
using System.Collections.Generic;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;

/// <summary>
/// Events from the Simulation to the clients, sent at the end of the tick. Not a bus: nothing on the server
/// subscribes to it. One buffer per joined peer keeps personal and common events in the order of publication.
/// </summary>
[ServerNetwork]
public class EventOutbox(NetMessageCodec codec, PeerUidMap peers)
{
    private const string OfflineReceiverLog = "{event} for player {uid} dropped: the player is not on any peer";
    private const string NoPeerBufferError = "Player {0} is bound to peer {1}, which has no event buffer.";
    private const string PeerExistsError = "Peer {0} already has an event buffer.";
    private const string PeerMissingError = "Peer {0} has no event buffer.";

    private readonly ILogger _log = LogFactory.GetForStatic<EventOutbox>();

    private readonly Dictionary<int, List<ReadOnlyMemory<byte>>> _bufferByPeerId = new();
    public IReadOnlyCollection<int> Peers => _bufferByPeerId.Keys;

    // Encoded once, here: every addressee gets the same bytes, and an event that cannot be serialized fails in
    // the Simulation that published it rather than at the end of the tick
    public void PublishToAll(Event @event)
    {
        byte[] encoded = codec.Encode(@event);
        foreach (List<ReadOnlyMemory<byte>> buffer in _bufferByPeerId.Values)
        {
            buffer.Add(encoded);
        }
    }

    // A receiver chosen earlier in the tick may have left by the time the event is published
    public void PublishTo(Event @event, string receiverUid)
    {
        byte[] encoded = codec.Encode(@event);
        if (!peers.TryGetPeerId(receiverUid, out int peerId))
        {
            _log.Information(OfflineReceiverLog, @event.GetType().Name, receiverUid);
            return;
        }
        if (!_bufferByPeerId.TryGetValue(peerId, out List<ReadOnlyMemory<byte>> buffer))
        {
            throw new InvalidOperationException(NoPeerBufferError.FormatWith(receiverUid, peerId));
        }

        buffer.Add(encoded);
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

    /// <summary>
    /// Writes the events section of the peer's events packet and empties its buffer.
    /// </summary>
    /// <returns>The number of events written: an events packet without events is not sent.</returns>
    public int DrainEvents(int peerId, IBufferWriter<byte> output)
    {
        if (!_bufferByPeerId.TryGetValue(peerId, out List<ReadOnlyMemory<byte>> buffer))
        {
            throw new InvalidOperationException(PeerMissingError.FormatWith(peerId));
        }

        codec.WriteSection(output, buffer);
        int count = buffer.Count;
        buffer.Clear();
        return count;
    }
}
