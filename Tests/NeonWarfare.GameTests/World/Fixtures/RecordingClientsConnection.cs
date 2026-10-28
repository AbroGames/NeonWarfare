using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;


namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// Plays Game's part: records every packet and, once <see cref="Loopback"/> is set, hands the packets of the host's
/// own peer to it synchronously, inside Send, as Game does. A disconnect is only recorded: the test enqueues the
/// <c>PeerDisconnected</c> that ENet would report.
/// </summary>
public class RecordingClientsConnection : IClientsConnection
{
    public const int HostPeer = 1;

    public record Sent(int PeerId, byte[] Packet);

    public List<Sent> Packets { get; } = [];

    // Set after the build: the fake goes into WorldDependencies before the container that holds the receiver exists
    public Action<ReadOnlyMemory<byte>>? Loopback { get; set; }

    public int? FailingPeer { get; set; }

    public List<int> Disconnected { get; } = [];

    public int? LocalPeerId { get; set; }

    public void Send(int peerId, ReadOnlySpan<byte> packet)
    {
        if (peerId == FailingPeer)
        {
            throw new InvalidOperationException($"send to peer {peerId} failed");
        }

        byte[] copy = packet.ToArray();
        Packets.Add(new Sent(peerId, copy));
        if (peerId == HostPeer)
        {
            Loopback?.Invoke(copy);
        }
    }

    public void Disconnect(int peerId) => Disconnected.Add(peerId);
}
