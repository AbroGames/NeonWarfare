using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// The join, displacement and leave of a peer, see Docs/Networking.md#join-and-leave.
/// A peer becomes <see cref="PeerStateTable.Joined"/> before the session handler publishes anything, so it gets the
/// events of its own join.
/// </summary>
[Server]
public class PeerSessions(IPeerSessionHandler sessionHandler, PeerStateTable peers, PeerGatekeeper gatekeeper)
{
    private readonly ILogger _log = LogFactory.GetForStatic<PeerSessions>();

    public void Join(int peerId, JoinRequestCommand command)
    {
        string name = command.GetType().Name;
        if (peers.TryGetJoined(peerId, out PeerStateTable.Joined joined))
        {
            _log.Warning("{command} from peer {peerId} dropped: the peer has already joined as {uid}",
                name, peerId, joined.Uid);
            return;
        }
        // This is checked first, so a cut peer cannot displace the player who is online with its uid.
        if (!peers.IsConnecting(peerId))
        {
            _log.Warning("{command} from peer {peerId} dropped: the peer is not waiting for a join", name, peerId);
            return;
        }

        try
        {
            JoinChecked(peerId, command);
        }
        // The model may stay half-changed, as with any facade. The peer must still not stay joined to a broken player.
        catch (Exception e)
        {
            _log.Error(e, "{handler} failed on {command} from peer {peerId}, the peer is rejected",
                sessionHandler.GetType().Name, name, peerId);
            bool wasJoined = peers.IsJoined(peerId);
            gatekeeper.Reject(peerId, JoinRejectReason.InternalError);
            if (wasJoined) peers.Release(peerId);
        }
    }

    private void JoinChecked(int peerId, JoinRequestCommand command)
    {
        string name = command.GetType().Name;
        if (!sessionHandler.ValidateJoin(command, out JoinRejectReason reason))
        {
            _log.Warning("{command} from peer {peerId} rejected by {handler}: {reason}",
                name, peerId, sessionHandler.GetType().Name, reason);
            gatekeeper.Reject(peerId, reason);
            return;
        }

        PlayerUid uid = command.Uid;
        if (peers.TryGetPeerIdByUid(uid, out int oldPeerId))
        {
            // The host cannot reconnect from another peer, so displacing it would lock it out of its own world.
            if (gatekeeper.IsLocal(oldPeerId))
            {
                _log.Warning("Peer {peerId} joins as {uid}, which is the host's own: rejected", peerId, uid);
                gatekeeper.Reject(peerId, JoinRejectReason.UidInUse);
                return;
            }

            // Displacement lets a crashed client come back before ENet notices that the old connection is gone.
            _log.Information("Peer {newPeerId} joins as {uid}, displacing peer {oldPeerId}", peerId, uid, oldPeerId);
            gatekeeper.Disconnect(oldPeerId);
            // The uid is released before Leave, so a throwing handler cannot keep it from the new peer.
            peers.Release(oldPeerId);
            sessionHandler.Leave(uid, LeaveReason.Displaced);
        }

        peers.Join(peerId, uid);
        sessionHandler.Join(command);
    }

    /// <summary>The peer is removed before <c>Leave</c>, so a throwing handler cannot leave it behind.</summary>
    public void Disconnected(int peerId)
    {
        switch (peers.Remove(peerId))
        {
            case PeerStateTable.Joined joined:
                sessionHandler.Leave(joined.Uid, LeaveReason.Disconnected);
                break;
            case PeerStateTable.Leaving leaving:
                sessionHandler.Leave(leaving.Uid, LeaveReason.Disconnected);
                break;
            default:
                _log.Debug("Peer {peerId} disconnected before joining", peerId);
                break;
        }
    }
}
