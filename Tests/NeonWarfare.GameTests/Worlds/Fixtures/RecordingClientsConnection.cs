using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;


namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// Plays the transport's part: records every packet and, once <see cref="Loopback"/> is set, hands the packets of the
/// host's own peer to it synchronously, inside Send, as the host's transport does; the commands go to
/// <see cref="CommandLoopback"/> the same way. The packets of a remote peer go to its entry in
/// <see cref="Receivers"/>, also inside Send: ENet keeps the order of a reliable channel, so a client World gets them
/// as if in <c>poll</c>. A join rejection is only recorded: the transport reads it itself, it never reaches a World.
/// A disconnect is only recorded too: the test enqueues the <c>PeerDisconnected</c> that ENet would report.
/// </summary>
public class RecordingClientsConnection : IClientsConnection, IServerConnection
{
    public const int HostPeer = 1;

    public record Sent(int PeerId, byte[] Packet);

    public List<Sent> Packets { get; } = [];

    public List<byte[]> Commands { get; } = [];

    // Set after the build: the fake goes into WorldDependencies before the container that holds the receiver exists
    public Action<ReadOnlyMemory<byte>>? Loopback { get; set; }

    public Action<ReadOnlyMemory<byte>>? CommandLoopback { get; set; }

    public Dictionary<int, Action<ReadOnlyMemory<byte>>> Receivers { get; } = [];

    public int? FailingPeer { get; set; }

    // Null: every packet to FailingPeer fails
    public ServerPacketKind? FailingKind { get; set; }

    public List<int> Disconnected { get; } = [];

    public int? LocalPeerId { get; set; }

    public void Send(int peerId, ReadOnlySpan<byte> packet)
    {
        if (peerId == FailingPeer && (FailingKind == null || packet[0] == (byte) FailingKind))
        {
            throw new InvalidOperationException($"send to peer {peerId} failed");
        }

        byte[] copy = packet.ToArray();
        Packets.Add(new Sent(peerId, copy));
        if (copy[0] == (byte) ServerPacketKind.JoinRejected) return;
        if (peerId == HostPeer)
        {
            Loopback?.Invoke(copy);
        }
        else if (Receivers.TryGetValue(peerId, out Action<ReadOnlyMemory<byte>>? receiver))
        {
            receiver(copy);
        }
    }

    public void Send(ReadOnlySpan<byte> packet)
    {
        byte[] copy = packet.ToArray();
        Commands.Add(copy);
        CommandLoopback?.Invoke(copy);
    }

    public void Disconnect(int peerId) => Disconnected.Add(peerId);
}
