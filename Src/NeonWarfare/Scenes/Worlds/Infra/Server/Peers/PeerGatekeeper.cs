using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// Lets a connected peer in only through a join: one that has not joined by the deadline is disconnected, a rejected
/// one is told why and disconnected. A disconnected peer is cut off in <see cref="PeerStateTable"/>: until its
/// <c>peer_disconnected</c> it still sends, and <see cref="CommandDispatcher"/> drops all of it, so a rejected or
/// displaced peer cannot act, displace back included.
/// </summary>
[Server]
public class PeerGatekeeper(IClientsConnection clientsConnection, TimeProvider timeProvider, PeerStateTable peers)
{
    public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(10);

    private const string TimedOutLog = "Peer {peerId} has not joined in {timeout}, disconnecting";
    private const string RejectedLog = "Peer {peerId} rejected: {reason}";

    private readonly ILogger _log = LogFactory.GetForStatic<PeerGatekeeper>();

    public void StartHandshake(int peerId) => peers.Connect(peerId, timeProvider.GetUtcNow() + HandshakeTimeout);

    public void DisconnectExpired()
    {
        foreach (int peerId in peers.ExpiredConnecting(timeProvider.GetUtcNow()))
        {
            _log.Information(TimedOutLog, peerId, HandshakeTimeout);
            Disconnect(peerId);
        }
    }

    public void Reject(int peerId, JoinRejectReason reason)
    {
        if (peers.IsCut(peerId)) return;

        _log.Warning(RejectedLog, peerId, reason);
        // A failed send must not leave the rejected peer connected
        try
        {
            clientsConnection.Reject(peerId, reason);
        }
        finally
        {
            Disconnect(peerId);
        }
    }

    public void Disconnect(int peerId)
    {
        if (peers.Disconnect(peerId)) clientsConnection.Disconnect(peerId);
    }

    public bool IsLocal(int peerId) => clientsConnection.LocalPeerId == peerId;
}
