using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The connection of a remote client, which sends the join once connected and has no World until the join snapshot.
/// </summary>
public sealed class ClientTransport : IServerConnection
{
    private const int ServerPeer = Consts.Global.ServerId;

    private readonly INetwork _network;
    private readonly NetMessageCodec _codec;
    private readonly ProtocolHash _protocolHash;
    private readonly LocalPlayer _localPlayer;
    private readonly ILocalPlayerOwner _owner;
    private readonly Action<long, ReadOnlyMemory<byte>> _snapshotReceived;

    private readonly ILogger _log = LogFactory.GetForStatic<ClientTransport>();

    private World.IClientEndpoint _endpoint;
    private bool _isBroken;

    /// <summary>
    /// A packet from the server could not be read or applied. Raised once, and the failure is already logged.
    /// Every later packet is dropped.
    /// </summary>
    public event Action ConnectionBroken;

    /// <param name="snapshotReceived">Builds the World from the tick and the records of the join snapshot and calls
    /// <see cref="Enter"/>. It must do so inside the call, before the tick update of the join tick arrives.</param>
    public ClientTransport(
        INetwork network, NetMessageCodec codec, ProtocolHash protocolHash, LocalPlayer localPlayer,
        ILocalPlayerOwner owner, Action<long, ReadOnlyMemory<byte>> snapshotReceived)
    {
        _network = network;
        _codec = codec;
        _protocolHash = protocolHash;
        _localPlayer = localPlayer;
        _owner = owner;
        _snapshotReceived = snapshotReceived;
        _network.ConnectedToServer += OnConnectedToServer;
        _network.PacketReceived += OnPacketReceived;
    }

    public void Enter(World.IClientEndpoint endpoint)
    {
        if (_endpoint != null) throw new InvalidOperationException("The client already has a World");

        _endpoint = endpoint;
    }

    /// <summary>
    /// A failed send is only logged, because the connection is going down and <c>Network</c> reports its end.
    /// </summary>
    public void Send(ReadOnlySpan<byte> packet) => _network.Send(ServerPeer, packet);

    private void OnConnectedToServer() => Send(_codec.Encode(_localPlayer.ToJoinRequest(_protocolHash)));

    private void OnPacketReceived(int peerId, byte[] packet)
    {
        if (peerId != ServerPeer)
        {
            _log.Warning("Packet from peer {peerId} dropped: not from the server", peerId);
            return;
        }

        if (_isBroken)
        {
            _log.Debug("Packet from the server dropped: the connection is already broken");
            return;
        }

        try
        {
            Receive(packet);
        }
        // Any failure ends the session, see Docs/Networking.md#packets-server--client.
        catch (Exception e)
        {
            _log.Error(e, "A packet from the server cannot be read or applied");
            _isBroken = true;
            ConnectionBroken?.Invoke();
        }
    }

    // Throws NetMessageFormatException when the packet is broken.
    private void Receive(byte[] packet)
    {
        (ServerPacketKind kind, ReadOnlyMemory<byte> body) = ServerPackets.Read(packet);
        switch (kind)
        {
            case ServerPacketKind.Disconnecting:
                DisconnectReason reason = ServerPackets.ReadDisconnecting(body.Span);
                _log.Information("The server is disconnecting: {reason}", reason);
                _owner.Disconnected(reason);
                break;
            case ServerPacketKind.Snapshot when _endpoint == null:
                (long snapshotTick, ReadOnlyMemory<byte> records) = ServerPackets.ReadSnapshot(body);
                _snapshotReceived(snapshotTick, records);
                break;
            case ServerPacketKind.Snapshot:
                throw new NetMessageFormatException("A snapshot for a client that already has a World.");
            case ServerPacketKind.TickUpdate when _endpoint == null:
                _log.Debug("Packet from peer {peerId} dropped: there is no World yet", ServerPeer);
                break;
            case ServerPacketKind.TickUpdate:
                (long tick, ReadOnlyMemory<byte> state, ReadOnlyMemory<byte> events) =
                    ServerPackets.ReadTickUpdate(body);
                _endpoint.ReceiveTickUpdate(tick, state, events);
                break;
        }
    }
}
