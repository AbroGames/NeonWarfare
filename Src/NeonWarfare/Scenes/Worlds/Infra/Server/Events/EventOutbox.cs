using System;
using System.Buffers;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Events;

/// <summary>
/// Collects the events the Simulation publishes for the clients, see Docs/Networking.md#packets-server--client.
/// Not a bus: nothing on the server subscribes to it.
/// </summary>
[Server]
public class EventOutbox(NetMessageCodec codec, PeerStateTable peers)
{
    private readonly ILogger _log = LogFactory.GetForStatic<EventOutbox>();

    /// <summary>Encodes the event once for every addressee, see Docs/Networking.md#packets-server--client.</summary>
    public void PublishToAll(Event @event)
    {
        byte[] encoded = codec.Encode(@event);
        foreach (PeerStateTable.Joined joined in peers.AllJoined)
        {
            joined.Events.Add(encoded);
        }
    }

    /// <summary>
    /// The event is dropped when the receiver is not joined, because a receiver chosen earlier in the tick may have
    /// left since.
    /// </summary>
    public void PublishTo(Event @event, string receiverUid)
    {
        byte[] encoded = codec.Encode(@event);
        if (!peers.TryGetJoinedByUid(receiverUid, out PeerStateTable.Joined joined))
        {
            _log.Information("{event} for player {uid} dropped: the player is not on any peer",
                @event.GetType().Name, receiverUid);
            return;
        }

        joined.Events.Add(encoded);
    }

    /// <summary>
    /// Writes the events section of the peer's events packet and empties its buffer.
    /// </summary>
    /// <returns>The number of events written. An events packet without events is not sent.</returns>
    public int DrainEvents(int peerId, IBufferWriter<byte> output)
    {
        if (!peers.TryGetJoined(peerId, out PeerStateTable.Joined joined))
        {
            throw new InvalidOperationException($"Peer {peerId} has not joined, so it has no events.");
        }

        codec.WriteSection(output, joined.Events);
        int count = joined.Events.Count;
        joined.Events.Clear();
        return count;
    }
}
