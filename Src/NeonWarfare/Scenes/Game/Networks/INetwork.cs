using System;

namespace NeonWarfare.Scenes.Game.Networks;

/// <summary>The part of <see cref="Network"/> the transports use.</summary>
public interface INetwork
{
    event Action<int> PeerConnected;
    event Action<int> PeerDisconnected;
    event Action<int, byte[]> PacketReceived;
    event Action ConnectedToServer;

    /// <returns><c>false</c> when the packet is not sent. The failure is already logged.</returns>
    bool Send(int peerId, ReadOnlySpan<byte> packet);

    void Disconnect(int peerId);
}
