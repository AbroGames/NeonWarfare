using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game.Transport;

/// <summary>
/// The connection of a remote client. Until it has a World the client is connecting: it sends the join once
/// connected and waits for the join snapshot, which <paramref name="snapshotReceived"/> builds the World from.
/// </summary>
public sealed class ClientTransport : IServerConnection
{
    private const int ServerPeer = Consts.Global.ServerId;

    private const string NoWorldLog = "Packet from peer {peerId} dropped: there is no World yet";
    private const string NotFromServerLog = "Packet from peer {peerId} dropped: not from the server";
    private const string BrokenServerPacketLog = "Packet from the server dropped";
    private const string JoinRejectedLog = "The server rejected the join: {reason}";
    private const string SecondWorldError = "The client already has a World";
    private const string SecondSnapshotError = "A snapshot for a client that already has a World.";

    private readonly INetwork _network;
    private readonly NetMessageCodec _codec;
    private readonly LocalPlayer _localPlayer;
    private readonly ILocalPlayerOwner _owner;
    private readonly Action<ReadOnlyMemory<byte>> _snapshotReceived;

    private readonly ILogger _log = LogFactory.GetForStatic<ClientTransport>();

    private World _world;

    /// <param name="snapshotReceived">Builds the World inside the call, before the events packet of the join tick
    /// arrives, and calls <see cref="Enter"/>.</param>
    public ClientTransport(
        INetwork network, NetMessageCodec codec, LocalPlayer localPlayer, ILocalPlayerOwner owner,
        Action<ReadOnlyMemory<byte>> snapshotReceived)
    {
        _network = network;
        _codec = codec;
        _localPlayer = localPlayer;
        _owner = owner;
        _snapshotReceived = snapshotReceived;
        _network.ConnectedToServer += OnConnectedToServer;
        _network.PacketReceived += OnPacketReceived;
    }

    public void Enter(World world)
    {
        if (_world != null) throw new InvalidOperationException(SecondWorldError);

        _world = world;
    }

    public void Send(ReadOnlySpan<byte> packet) => _network.Send(ServerPeer, packet);

    private void OnConnectedToServer() => Send(_codec.Encode(_localPlayer.ToJoinRequest(_codec.ProtocolHash)));

    private void OnPacketReceived(int peerId, byte[] packet)
    {
        if (peerId != ServerPeer)
        {
            _log.Warning(NotFromServerLog, peerId);
            return;
        }

        try
        {
            Receive(packet);
        }
        catch (NetMessageFormatException e)
        {
            _log.Error(e, BrokenServerPacketLog);
        }
    }

    /// <exception cref="NetMessageFormatException">The packet is broken.</exception>
    private void Receive(byte[] packet)
    {
        (ServerPacketKind kind, ReadOnlyMemory<byte> body) = ServerPackets.Read(packet);
        switch (kind)
        {
            case ServerPacketKind.JoinRejected:
                JoinRejectReason reason = ServerPackets.ReadJoinRejected(body.Span);
                _log.Error(JoinRejectedLog, reason);
                _owner.JoinRejected(reason);
                break;
            case ServerPacketKind.Snapshot when _world == null:
                _snapshotReceived(body);
                break;
            case ServerPacketKind.Snapshot:
                throw new NetMessageFormatException(SecondSnapshotError);
            case ServerPacketKind.State or ServerPacketKind.Events when _world == null:
                _log.Debug(NoWorldLog, ServerPeer);
                break;
            case ServerPacketKind.State:
                _world.ReceiveState(body);
                break;
            case ServerPacketKind.Events:
                _world.ReceiveEvents(body);
                break;
        }
    }
}
