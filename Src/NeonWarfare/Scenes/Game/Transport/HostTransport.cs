using System;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game.Transport;

/// <summary>
/// The host is the server and a client in one process: its own peer is the server's, and its packets are looped
/// back into its World synchronously, inside the call. The remote peers, if any, go to <paramref name="remote"/>.
/// </summary>
public sealed class HostTransport(
    World world,
    NetMessageCodec codec,
    ILocalPlayerOwner owner,
    ServerTransport remote
    ) : IClientsConnection, IServerConnection
{
    private const int LocalPeer = Consts.Global.ServerId;

    private const string JoinRejectedLog = "The server rejected the join: {reason}";
    private const string LocalDisconnectedLog = "The host's own peer is disconnected: the host has no player";
    private const string NoNetworkError = "Peer {0} is not the host's own, but there is no network";

    private readonly ILogger _log = LogFactory.GetForStatic<HostTransport>();

    public int? LocalPeerId => LocalPeer;

    /// <summary>
    /// The host's own peer first connects, as a remote one would, so its join passes the same gatekeeper.
    /// </summary>
    public void SendJoinRequest(LocalPlayer localPlayer)
    {
        world.StartHandshake(LocalPeer);
        ((IServerConnection) this).Send(codec.Encode(localPlayer.ToJoinRequest(codec.ProtocolHash)));
    }

    void IClientsConnection.Send(int peerId, ReadOnlySpan<byte> packet)
    {
        if (peerId != LocalPeer)
        {
            Remote(peerId).Send(peerId, packet);
            return;
        }

        if (packet.Length > 0 && packet[0] == (byte) ServerPacketKind.JoinRejected)
        {
            JoinRejectReason reason = JoinRejectedPacket.Read(packet);
            _log.Error(JoinRejectedLog, reason);
            owner.JoinRejected(reason);
            return;
        }
        world.ReceiveFromServer(packet.ToArray());
    }

    void IClientsConnection.Disconnect(int peerId)
    {
        if (peerId != LocalPeer)
        {
            Remote(peerId).Disconnect(peerId);
            return;
        }

        // Queued like any other disconnection, so it is processed in the next tick, not inside this one
        _log.Error(LocalDisconnectedLog);
        world.QueueDisconnection(peerId);
    }

    void IServerConnection.Send(ReadOnlySpan<byte> packet) => world.ReceiveFromClient(LocalPeer, packet.ToArray());

    private ServerTransport Remote(int peerId) =>
        remote ?? throw new InvalidOperationException(NoNetworkError.FormatWith(peerId));
}
