using System;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Events;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

/// <summary>
/// The life of a joined peer, kept in one place: after a valid join the peer is bound to its uid and gets its event
/// buffer, both before the join handler publishes anything, and loses them on its disconnection or displacement.
/// Called by <see cref="CommandDispatcher"/> in the tick, in the order of the inbox.
/// </summary>
[ServerNetwork]
public class PeerSessions(
    CommandHandlerRegistry handlers,
    PeerUidMap peers,
    EventOutbox outbox,
    PeerGatekeeper gatekeeper)
{
    private const string RejoinLog = "{command} from peer {peerId} dropped: the peer has already joined as {uid}";
    private const string JoinRejectedLog = "{command} from peer {peerId} rejected by {handler}: {reason}";
    private const string JoinValidateFailedLog = "{handler} failed to validate {command}, the peer is rejected";
    private const string JoinProcessFailedLog = "{handler} failed to process {command}, the peer is rejected";
    private const string HostUidLog = "Peer {peerId} joins as {uid}, which is the host's own: rejected";
    private const string DisplacedLog = "Peer {newPeerId} joins as {uid}, displacing peer {oldPeerId}";
    private const string NotJoinedLeftLog = "Peer {peerId} disconnected before joining";
    private const string NoJoinHandlerError =
        "JoinRequestCommand from peer {0} passed the inbox, but there is no join handler: the whitelist is miswired.";

    private readonly ILogger _log = LogFactory.GetForStatic<PeerSessions>();

    public void Join(int peerId, JoinRequestCommand command)
    {
        string name = command.GetType().Name;
        if (peers.TryGetUid(peerId, out string joinedUid))
        {
            _log.Warning(RejoinLog, name, peerId, joinedUid);
            return;
        }
        // The inbox lets the join through only when there is a join handler, so this is a wiring error, not a peer's
        IJoinRequestHandler joinHandler = handlers.JoinHandler
            ?? throw new InvalidOperationException(NoJoinHandlerError.FormatWith(peerId));

        string handlerName = joinHandler.GetType().Name;
        JoinRejectReason reason;
        bool valid;
        try
        {
            valid = joinHandler.Validate(command, out reason);
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
        if (peers.TryGetPeerId(uid, out int oldPeerId))
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
            // Leave unbinds the old peer even if it throws, and an unbound peer has no handshake deadline any more
            try
            {
                Leave(oldPeerId, uid);
            }
            finally
            {
                gatekeeper.Disconnect(oldPeerId);
            }
        }

        peers.Bind(uid, peerId);
        outbox.AddPeer(peerId);
        try
        {
            joinHandler.Process(command);
        }
        // The model may stay half-changed, as with any facade, but the peer must not stay joined to a broken player
        catch (Exception e)
        {
            _log.Error(e, JoinProcessFailedLog, handlerName, name);
            outbox.RemovePeer(peerId);
            peers.Unbind(peerId);
            gatekeeper.Reject(peerId, JoinRejectReason.InternalError);
        }
    }

    public void Disconnected(int peerId)
    {
        try
        {
            if (peers.TryGetUid(peerId, out string uid))
            {
                Leave(peerId, uid);
            }
            else
            {
                _log.Debug(NotJoinedLeftLog, peerId);
            }
        }
        finally
        {
            gatekeeper.Forget(peerId);
        }
    }

    // The buffer and the binding go even if the handler throws: a peer left bound would block its uid forever
    private void Leave(int peerId, string uid)
    {
        try
        {
            handlers.DisconnectedHandler.Process(uid);
        }
        finally
        {
            outbox.RemovePeer(peerId);
            peers.Unbind(peerId);
        }
    }
}
