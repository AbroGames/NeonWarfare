using System;
using System.Buffers;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Events;

/// <summary>
/// Events from the Simulation to the clients, sent at the end of the tick. Not a bus: nothing on the server
/// subscribes to it. Each joined peer has one buffer in <see cref="PeerStateTable"/>, so personal and common events
/// keep the order of publication.
/// </summary>
[Server]
public class EventOutbox(NetMessageCodec codec, PeerStateTable peers)
{
    private const string OfflineReceiverLog = "{event} for player {uid} dropped: the player is not on any peer";
    private const string NotJoinedError = "Peer {0} has not joined, so it has no events.";

    private readonly ILogger _log = LogFactory.GetForStatic<EventOutbox>();

    // Encoded once, here: every addressee gets the same bytes, and an event that cannot be serialized fails in
    // the Simulation that published it rather than at the end of the tick
    public void PublishToAll(Event @event)
    {
        byte[] encoded = codec.Encode(@event);
        foreach (PeerStateTable.Joined joined in peers.AllJoined)
        {
            joined.Events.Add(encoded);
        }
    }

    // A receiver chosen earlier in the tick may have left by the time the event is published
    public void PublishTo(Event @event, string receiverUid)
    {
        byte[] encoded = codec.Encode(@event);
        if (!peers.TryGetJoinedByUid(receiverUid, out PeerStateTable.Joined joined))
        {
            _log.Information(OfflineReceiverLog, @event.GetType().Name, receiverUid);
            return;
        }

        joined.Events.Add(encoded);
    }

    /// <summary>
    /// Writes the events section of the peer's events packet and empties its buffer.
    /// </summary>
    /// <returns>The number of events written: an events packet without events is not sent.</returns>
    public int DrainEvents(int peerId, IBufferWriter<byte> output)
    {
        if (!peers.TryGetJoined(peerId, out PeerStateTable.Joined joined))
        {
            throw new InvalidOperationException(NotJoinedError.FormatWith(peerId));
        }

        codec.WriteSection(output, joined.Events);
        int count = joined.Events.Count;
        joined.Events.Clear();
        return count;
    }
}
