using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The host is the server and a client in one process: its own peer is the server's, and its packets are looped
/// back into its World synchronously, inside the call. The remote peers, if any, go to <paramref name="remote"/>.
/// </summary>
public sealed class HostTransport(
    World.IServerEndpoint server,
    World.IClientEndpoint client,
    NetMessageCodec codec,
    ProtocolHash protocolHash,
    ILocalPlayerOwner owner,
    ServerTransport remote
    ) : IClientsConnection, IServerConnection
{
    private const int LocalPeer = Consts.Global.ServerId;

    private readonly ILogger _log = LogFactory.GetForStatic<HostTransport>();

    public int? LocalPeerId => LocalPeer;

    /// <summary>
    /// The host's own peer first connects, as a remote one would, so its join passes the same gatekeeper.
    /// </summary>
    public void SendJoinRequest(LocalPlayer localPlayer)
    {
        server.StartHandshake(LocalPeer);
        ((IServerConnection) this).Send(codec.Encode(localPlayer.ToJoinRequest(protocolHash)));
    }

    void IClientsConnection.SendState(int peerId, ReadOnlySpan<byte> body) => Remote(peerId).SendState(peerId, body);

    void IClientsConnection.SendSnapshot(int peerId, ReadOnlySpan<byte> body) =>
        Remote(peerId).SendSnapshot(peerId, body);

    void IClientsConnection.SendEvents(int peerId, ReadOnlySpan<byte> body)
    {
        if (peerId == LocalPeer)
        {
            client.ReceiveEvents(body.ToArray());
            return;
        }
        Remote(peerId).SendEvents(peerId, body);
    }

    void IClientsConnection.Reject(int peerId, JoinRejectReason reason)
    {
        if (peerId == LocalPeer)
        {
            _log.Error("The server rejected the join: {reason}", reason);
            owner.JoinRejected(reason);
            return;
        }
        Remote(peerId).Reject(peerId, reason);
    }

    void IClientsConnection.Disconnect(int peerId)
    {
        if (peerId != LocalPeer)
        {
            Remote(peerId).Disconnect(peerId);
            return;
        }

        // Queued like any other disconnection, so it is processed in the next tick, not inside this one. A warning:
        // whatever cut the host off has already logged its error
        _log.Warning("The host's own peer is disconnected: the host has no player");
        server.QueueDisconnection(peerId);
    }

    void IServerConnection.Send(ReadOnlySpan<byte> packet) => server.ReceiveFromClient(LocalPeer, packet.ToArray());

    private ServerTransport Remote(int peerId)
    {
        if (peerId == LocalPeer)
        {
            throw new InvalidOperationException(
                $"Peer {peerId} is the host's own: its World is the server's and needs no state");
        }

        return remote
               ?? throw new InvalidOperationException($"Peer {peerId} is not the host's own, but there is no network");
    }
}
