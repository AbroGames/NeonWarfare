using System;
using System.Collections.Generic;
using System.Linq;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// Lets a connected peer in only through a join: one that has not joined by the deadline is disconnected, a rejected
/// one is told why and disconnected. Between the disconnect and its <c>peer_disconnected</c> the peer still sends,
/// so <see cref="CommandDispatcher"/> drops everything from a disconnecting peer: a rejected or displaced peer
/// must not act, displace back included.
/// </summary>
[Server]
public class PeerGatekeeper(IClientsConnection clientsConnection, TimeProvider timeProvider, PeerUidMap peers)
{
    public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(10);

    private const string TimedOutLog = "Peer {peerId} has not joined in {timeout}, disconnecting";
    private const string RejectedLog = "Peer {peerId} rejected: {reason}";

    private readonly ILogger _log = LogFactory.GetForStatic<PeerGatekeeper>();

    private readonly Dictionary<int, DateTimeOffset> _deadlineByPeerId = new();
    private readonly HashSet<int> _disconnecting = [];

    public void StartHandshake(int peerId)
    {
        _deadlineByPeerId[peerId] = timeProvider.GetUtcNow() + HandshakeTimeout;
    }

    public void DisconnectExpired()
    {
        if (_deadlineByPeerId.Count == 0) return;

        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach ((int peerId, DateTimeOffset deadline) in _deadlineByPeerId.ToList())
        {
            if (peers.TryGetUid(peerId, out _))
            {
                _deadlineByPeerId.Remove(peerId);
                continue;
            }
            if (now < deadline) continue;

            _log.Information(TimedOutLog, peerId, HandshakeTimeout);
            Disconnect(peerId);
        }
    }

    public void Reject(int peerId, JoinRejectReason reason)
    {
        if (_disconnecting.Contains(peerId)) return;

        _log.Warning(RejectedLog, peerId, reason);
        // A failed send must not leave the rejected peer connected
        try
        {
            clientsConnection.Send(peerId, JoinRejectedPacket.Write(reason));
        }
        finally
        {
            Disconnect(peerId);
        }
    }

    public void Disconnect(int peerId)
    {
        _deadlineByPeerId.Remove(peerId);
        if (!_disconnecting.Add(peerId)) return;

        clientsConnection.Disconnect(peerId);
    }
    
    public void Forget(int peerId)
    {
        _deadlineByPeerId.Remove(peerId);
        _disconnecting.Remove(peerId);
    }

    public bool IsDisconnecting(int peerId) => _disconnecting.Contains(peerId);

    public bool IsLocal(int peerId) => clientsConnection.LocalPeerId == peerId;


}
