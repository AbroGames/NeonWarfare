using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;


namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// What a packet carries: the World never sees the transport's kind byte.
/// A tick update is <see cref="State"/>, <see cref="Events"/> or both.
/// </summary>
[Flags]
public enum SentKind
{
    Snapshot = 1,
    State = 2,
    Events = 4,
    JoinRejected = 8
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

    // Body: the records of a snapshot, the reason byte of a rejection, the state section of a tick update.
    // Events: the events section of a tick update, empty otherwise.
    public record Sent(int PeerId, SentKind Kind, long Tick, byte[] Body, byte[] Events);

    public List<Sent> Packets { get; } = [];

    public List<byte[]> Commands { get; } = [];

    // Set after the build: the fake goes into WorldDependencies before the container that holds the receiver exists
    public Action<ReadOnlyMemory<byte>>? Loopback { get; set; }

    public Action<ReadOnlyMemory<byte>>? CommandLoopback { get; set; }

    public Dictionary<int, Action<Sent>> Receivers { get; } = [];

    public int? FailingPeer { get; set; }

    // Null: every packet to FailingPeer fails; otherwise every packet that carries any of these kinds
    public SentKind? FailingKind { get; set; }

    // A packet that carries any of these kinds throws, as a transport bug would
    public SentKind? ThrowingKind { get; set; }

    public List<int> Disconnected { get; } = [];

    public int? LocalPeerId { get; set; }

    public bool SendTickUpdate(int peerId, long tick, ReadOnlySpan<byte> state, ReadOnlySpan<byte> events)
    {
        if (state.IsEmpty && events.IsEmpty) throw new ArgumentException("A tick update with neither section");

        SentKind kind = (state.IsEmpty ? 0 : SentKind.State) | (events.IsEmpty ? 0 : SentKind.Events);
        return Record(new Sent(peerId, kind, tick, state.ToArray(), events.ToArray()));
    }

    public bool SendSnapshot(int peerId, long tick, ReadOnlySpan<byte> records) =>
        Record(new Sent(peerId, SentKind.Snapshot, tick, records.ToArray(), []));

    public bool Reject(int peerId, JoinRejectReason reason) =>
        Record(new Sent(peerId, SentKind.JoinRejected, 0, [(byte) reason], []));

    public void Send(ReadOnlySpan<byte> packet)
    {
        byte[] copy = packet.ToArray();
        Commands.Add(copy);
        CommandLoopback?.Invoke(copy);
    }

    public void Disconnect(int peerId) => Disconnected.Add(peerId);

    private bool Record(Sent sent)
    {
        if (ThrowingKind is { } throwing && (sent.Kind & throwing) != 0)
        {
            throw new InvalidOperationException($"{sent.Kind} throws");
        }
        if (sent.PeerId == FailingPeer && (FailingKind == null || (sent.Kind & FailingKind) != 0)) return false;

        Packets.Add(sent);
        if (sent.Kind == SentKind.JoinRejected) return true;
        if (sent.PeerId == HostPeer)
        {
            if (sent.Events.Length > 0) Loopback?.Invoke(sent.Events);
        }
        else if (Receivers.TryGetValue(sent.PeerId, out Action<Sent>? receiver))
        {
            receiver(sent);
        }
        return true;
    }
}
