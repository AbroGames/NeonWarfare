using System;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// The life of a joined peer: after a valid join it becomes <see cref="PeerStateTable.Joined"/> before the session
/// handler publishes anything, and the handler's <c>Leave</c> runs once, on its disconnection or displacement.
/// Called by <see cref="CommandDispatcher"/> in the tick, in the order of the inbox.
/// </summary>
[Server]
public class PeerSessions(CommandHandlerRegistry handlers, PeerStateTable peers, PeerGatekeeper gatekeeper)
{
    private const string RejoinLog = "{command} from peer {peerId} dropped: the peer has already joined as {uid}";
    private const string NotConnectingLog = "{command} from peer {peerId} dropped: the peer is not waiting for a join";
    private const string JoinRejectedLog = "{command} from peer {peerId} rejected by {handler}: {reason}";
    private const string JoinValidateFailedLog = "{handler} failed to validate {command}, the peer is rejected";
    private const string JoinFailedLog = "{handler} failed to process {command}, the peer is rejected";
    private const string HostUidLog = "Peer {peerId} joins as {uid}, which is the host's own: rejected";
    private const string DisplacedLog = "Peer {newPeerId} joins as {uid}, displacing peer {oldPeerId}";
    private const string NotJoinedLeftLog = "Peer {peerId} disconnected before joining";
    private const string NoSessionHandlerError =
        "JoinRequestCommand from peer {0} passed the inbox, but there is no session handler: "
        + "the whitelist is miswired.";

    private readonly ILogger _log = LogFactory.GetForStatic<PeerSessions>();

    public void Join(int peerId, JoinRequestCommand command)
    {
        string name = command.GetType().Name;
        if (peers.TryGetJoined(peerId, out PeerStateTable.Joined joined))
        {
            _log.Warning(RejoinLog, name, peerId, joined.Uid);
            return;
        }
        // Checked before anything is done for the join: a cut peer must not displace the player online with its uid
        if (!peers.IsConnecting(peerId))
        {
            _log.Warning(NotConnectingLog, name, peerId);
            return;
        }
        // The inbox lets the join through only when there is a session handler, so this is a wiring error, not a peer's
        IPeerSessionHandler sessionHandler = handlers.SessionHandler
            ?? throw new InvalidOperationException(NoSessionHandlerError.FormatWith(peerId));

        string handlerName = sessionHandler.GetType().Name;
        JoinRejectReason reason;
        bool valid;
        try
        {
            valid = sessionHandler.ValidateJoin(command, out reason);
            if (!valid)
            {
                _log.Warning(JoinRejectedLog, name, peerId, handlerName, reason);
            }
        }
        // Caught here rather than in CommandDispatcher: the peer still has to be rejected
        catch (Exception e)
        {
            _log.Error(e, JoinValidateFailedLog, handlerName, name);
            valid = false;
            reason = JoinRejectReason.InternalError;
        }

        if (!valid)
        {
            gatekeeper.Reject(peerId, reason);
            return;
        }

        string uid = command.Uid;
        if (peers.TryGetPeerIdByUid(uid, out int oldPeerId))
        {
            // The host cannot be reconnected from another peer, so displacing it would only lock it out of its world
            if (gatekeeper.IsLocal(oldPeerId))
            {
                _log.Warning(HostUidLog, peerId, uid);
                gatekeeper.Reject(peerId, JoinRejectReason.UidInUse);
                return;
            }

            // Otherwise a crashed client could not come back until ENet notices the old connection is gone
            _log.Information(DisplacedLog, peerId, uid, oldPeerId);
            gatekeeper.Disconnect(oldPeerId);
            // A throwing handler must not keep the uid from the new peer
            try
            {
                sessionHandler.Leave(uid, LeaveReason.Displaced);
            }
            finally
            {
                peers.Release(oldPeerId);
            }
        }

        peers.Join(peerId, uid);
        try
        {
            sessionHandler.Join(command);
        }
        // The model may stay half-changed, as with any facade, but the peer must not stay joined to a broken player
        catch (Exception e)
        {
            _log.Error(e, JoinFailedLog, handlerName, name);
            try
            {
                gatekeeper.Reject(peerId, JoinRejectReason.InternalError);
            }
            finally
            {
                peers.Release(peerId);
            }
        }
    }

    // Removed before Leave, so a throwing handler cannot leave the peer behind
    public void Disconnected(int peerId)
    {
        switch (peers.Remove(peerId))
        {
            case PeerStateTable.Joined joined:
                handlers.SessionHandler.Leave(joined.Uid, LeaveReason.Disconnected);
                break;
            case PeerStateTable.Leaving leaving:
                handlers.SessionHandler.Leave(leaving.Uid, LeaveReason.Disconnected);
                break;
            default:
                _log.Debug(NotJoinedLeftLog, peerId);
                break;
        }
    }
}
