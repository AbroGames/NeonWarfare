using System;
using System.Buffers;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>The connection of a server World to its remote peers.</summary>
public sealed class ServerTransport : IClientsConnection, IDisposable
{
    private readonly INetwork _network;
    private readonly World.IServerEndpoint _endpoint;
    private readonly ArrayBufferWriter<byte> _packet = new();

    public ServerTransport(INetwork network, World.IServerEndpoint endpoint)
    {
        _network = network;
        _endpoint = endpoint;
        _network.PeerConnected += OnPeerConnected;
        _network.PeerDisconnected += OnPeerDisconnected;
        _network.PacketReceived += OnPacketReceived;
    }

    public int? LocalPeerId => null;

    public bool SendTickUpdate(int peerId, long tick, ReadOnlySpan<byte> state, ReadOnlySpan<byte> events)
    {
        _packet.ResetWrittenCount();
        ServerPackets.WriteTickUpdate(tick, state, events, _packet);
        return _network.Send(peerId, _packet.WrittenSpan);
    }

    public bool SendSnapshot(int peerId, long tick, ReadOnlySpan<byte> records)
    {
        _packet.ResetWrittenCount();
        ServerPackets.WriteSnapshot(tick, records, _packet);
        return _network.Send(peerId, _packet.WrittenSpan);
    }

    public bool SendDisconnectReason(int peerId, DisconnectReason reason)
    {
        _packet.ResetWrittenCount();
        ServerPackets.WriteDisconnecting(reason, _packet);
        return _network.Send(peerId, _packet.WrittenSpan);
    }

    public void Disconnect(int peerId) => _network.Disconnect(peerId);

    /// <summary>
    /// Detaches from the network when the World failed to be built, because the network outlives that World.
    /// </summary>
    public void Dispose()
    {
        _network.PeerConnected -= OnPeerConnected;
        _network.PeerDisconnected -= OnPeerDisconnected;
        _network.PacketReceived -= OnPacketReceived;
    }

    private void OnPeerConnected(int peerId) => _endpoint.StartHandshake(peerId);

    private void OnPeerDisconnected(int peerId) => _endpoint.QueueDisconnection(peerId);

    private void OnPacketReceived(int peerId, byte[] packet) => _endpoint.ReceiveFromClient(peerId, packet);
}
