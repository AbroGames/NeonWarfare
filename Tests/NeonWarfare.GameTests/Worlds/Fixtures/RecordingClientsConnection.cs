using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;


namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// Which <see cref="IClientsConnection"/> method sent a packet: the World never sees the transport's kind byte.
/// </summary>
public enum SentKind
{
    State,
    Snapshot,
    Events,
    JoinRejected
}

/// <summary>
/// Plays the transport's part: records every packet and, once <see cref="Loopback"/> is set, hands the events of the
/// host's own peer to it synchronously, inside the call, as the host's transport does; the commands go to
/// <see cref="CommandLoopback"/> the same way. The packets of a remote peer go to its entry in
/// <see cref="Receivers"/>, also inside the call: ENet keeps the order of a reliable channel, so a client World gets
/// them as if in <c>poll</c>. A join rejection is only recorded, its body is the reason byte: the transport reads it
/// itself, it never reaches a World. A disconnect is only recorded too: the test enqueues the
/// <c>PeerDisconnected</c> that ENet would report.
/// </summary>
public class RecordingClientsConnection : IClientsConnection, IServerConnection
{
    public const int HostPeer = 1;

    public record Sent(int PeerId, SentKind Kind, byte[] Body);

    public List<Sent> Packets { get; } = [];

    public List<byte[]> Commands { get; } = [];

    // Set after the build: the fake goes into WorldDependencies before the container that holds the receiver exists
    public Action<ReadOnlyMemory<byte>>? Loopback { get; set; }

    public Action<ReadOnlyMemory<byte>>? CommandLoopback { get; set; }

    public Dictionary<int, Action<SentKind, ReadOnlyMemory<byte>>> Receivers { get; } = [];

    public int? FailingPeer { get; set; }

    // Null: every packet to FailingPeer fails
    public SentKind? FailingKind { get; set; }

    public List<int> Disconnected { get; } = [];

    public int? LocalPeerId { get; set; }

    public void SendState(int peerId, ReadOnlySpan<byte> body) => Record(peerId, SentKind.State, body);

    public void SendSnapshot(int peerId, ReadOnlySpan<byte> body) => Record(peerId, SentKind.Snapshot, body);

    public void SendEvents(int peerId, ReadOnlySpan<byte> body) => Record(peerId, SentKind.Events, body);

    public void Reject(int peerId, JoinRejectReason reason) =>
        Record(peerId, SentKind.JoinRejected, [(byte) reason]);

    public void Send(ReadOnlySpan<byte> packet)
    {
        byte[] copy = packet.ToArray();
        Commands.Add(copy);
        CommandLoopback?.Invoke(copy);
    }

    public void Disconnect(int peerId) => Disconnected.Add(peerId);

    private void Record(int peerId, SentKind kind, ReadOnlySpan<byte> body)
    {
        if (peerId == FailingPeer && (FailingKind == null || kind == FailingKind))
        {
            throw new InvalidOperationException($"send to peer {peerId} failed");
        }

        byte[] copy = body.ToArray();
        Packets.Add(new Sent(peerId, kind, copy));
        if (kind == SentKind.JoinRejected) return;
        if (peerId == HostPeer)
        {
            if (kind == SentKind.Events) Loopback?.Invoke(copy);
        }
        else if (Receivers.TryGetValue(peerId, out Action<SentKind, ReadOnlyMemory<byte>>? receiver))
        {
            receiver(kind, copy);
        }
    }
}
