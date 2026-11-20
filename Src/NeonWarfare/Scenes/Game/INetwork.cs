using System;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// What the transports use of <see cref="Network"/>.
/// </summary>
public interface INetwork
{
    event Action<int> PeerConnected;
    event Action<int> PeerDisconnected;
    event Action<int, byte[]> PacketReceived;
    event Action ConnectedToServer;

    void Send(int peerId, ReadOnlySpan<byte> packet);

    void Disconnect(int peerId);
}
