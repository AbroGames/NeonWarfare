using System;
using System.Buffers;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Transport;

/// <summary>
/// The remote peers of a server World. The server is opened only once its World is built, so nothing reaches a
/// World that does not exist yet.
/// </summary>
public sealed class ServerTransport : IClientsConnection, IDisposable
{
    private readonly INetwork _network;
    private readonly World _world;
    private readonly ArrayBufferWriter<byte> _packet = new();

    public ServerTransport(INetwork network, World world)
    {
        _network = network;
        _world = world;
        _network.PeerConnected += OnPeerConnected;
        _network.PeerDisconnected += OnPeerDisconnected;
        _network.PacketReceived += OnPacketReceived;
    }

    public int? LocalPeerId => null;

    public void SendState(int peerId, ReadOnlySpan<byte> body) => Send(peerId, ServerPacketKind.State, body);

    public void SendSnapshot(int peerId, ReadOnlySpan<byte> body) => Send(peerId, ServerPacketKind.Snapshot, body);

    public void SendEvents(int peerId, ReadOnlySpan<byte> body) => Send(peerId, ServerPacketKind.Events, body);

    public void Reject(int peerId, JoinRejectReason reason)
    {
        _packet.ResetWrittenCount();
        ServerPackets.WriteJoinRejected(reason, _packet);
        _network.Send(peerId, _packet.WrittenSpan);
    }

    public void Disconnect(int peerId) => _network.Disconnect(peerId);

    /// <summary>
    /// Detaches from the network, for a World that failed to be built: the network outlives it.
    /// </summary>
    public void Dispose()
    {
        _network.PeerConnected -= OnPeerConnected;
        _network.PeerDisconnected -= OnPeerDisconnected;
        _network.PacketReceived -= OnPacketReceived;
    }

    private void Send(int peerId, ServerPacketKind kind, ReadOnlySpan<byte> body)
    {
        _packet.ResetWrittenCount();
        ServerPackets.Write(kind, body, _packet);
        _network.Send(peerId, _packet.WrittenSpan);
    }

    private void OnPeerConnected(int peerId) => _world.StartHandshake(peerId);

    private void OnPeerDisconnected(int peerId) => _world.QueueDisconnection(peerId);

    private void OnPacketReceived(int peerId, byte[] packet) => _world.ReceiveFromClient(peerId, packet);
}
