using NeonWarfare.Scenes.Game;

namespace NeonWarfare.GameTests.Game.Fixtures;

/// <summary>
/// Plays ENet's part: records what is sent and disconnected, raises the connection events and the packets on demand.
/// What is sent also goes to <see cref="Wire"/>, inside the call, as ENet delivers a reliable channel in order.
/// </summary>
public class FakeNetwork : INetwork
{
    public record Sent(int PeerId, byte[] Packet);

    public event Action<int>? PeerConnected;
    public event Action<int>? PeerDisconnected;
    public event Action<int, byte[]>? PacketReceived;
    public event Action? ConnectedToServer;

    public List<Sent> Packets { get; } = [];

    public List<int> Disconnected { get; } = [];

    public Action<int, byte[]>? Wire { get; set; }

    public void Send(int peerId, ReadOnlySpan<byte> packet)
    {
        byte[] copy = packet.ToArray();
        Packets.Add(new Sent(peerId, copy));
        Wire?.Invoke(peerId, copy);
    }

    public void Disconnect(int peerId) => Disconnected.Add(peerId);

    public void Connect(int peerId) => PeerConnected?.Invoke(peerId);

    public void Drop(int peerId) => PeerDisconnected?.Invoke(peerId);

    public void Receive(int peerId, byte[] packet) => PacketReceived?.Invoke(peerId, packet);

    public void ConnectToServer() => ConnectedToServer?.Invoke();
}
