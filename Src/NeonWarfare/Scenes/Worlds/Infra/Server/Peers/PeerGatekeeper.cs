using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// Lets a connected peer in only through a join, see Docs/Networking.md#join-and-leave.
/// A disconnected peer is cut off in <see cref="PeerStateTable"/> at once.
/// It may keep sending until its <c>peer_disconnected</c>, but <see cref="CommandDispatcher"/> drops all of it, so a
/// rejected or displaced peer cannot act, nor displace the new peer back.
/// </summary>
[Server]
public class PeerGatekeeper(IClientsConnection clientsConnection, TimeProvider timeProvider, PeerStateTable peers)
{
    public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger _log = LogFactory.GetForStatic<PeerGatekeeper>();

    public void StartHandshake(int peerId) => peers.Connect(peerId, timeProvider.GetUtcNow() + HandshakeTimeout);

    public void DisconnectExpired()
    {
        foreach (int peerId in peers.ExpiredConnecting(timeProvider.GetUtcNow()))
        {
            _log.Information("Peer {peerId} has not joined in {timeout}, disconnecting", peerId, HandshakeTimeout);
            Disconnect(peerId);
        }
    }

    public void Reject(int peerId, JoinRejectReason reason)
    {
        if (peers.IsCut(peerId)) return;

        _log.Warning("Peer {peerId} rejected: {reason}", peerId, reason);
        // The peer is disconnected whether the reason reached it or not.
        clientsConnection.Reject(peerId, reason);
        Disconnect(peerId);
    }

    public void Disconnect(int peerId)
    {
        if (peers.Disconnect(peerId)) clientsConnection.Disconnect(peerId);
    }
}
