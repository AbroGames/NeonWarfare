using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork;

/// <summary>
/// Drains <see cref="CommandInbox"/> in the server tick and hands every entry to the handler of its command.
/// The handlers are collected once, by the composition root, since MS.DI cannot inject "every
/// <see cref="IPlayerCommandHandler{TCommand}"/>". "Joined or not" is decided here, at tick time rather than on
/// arrival: a join and a chat command of one peer in one tick must both pass, in order. A handler gets the bound uid
/// and looks its own state up by it.<br/><br/>
/// The life of a joined peer is kept here, in one place: after a valid join the peer is bound to its uid and gets
/// its event buffer, both before the join handler publishes anything, and loses them on its disconnection.
/// </summary>
[ServerNetwork]
public class CommandDispatcher(CommandInbox inbox, PeerUidMap peers, EventOutbox outbox, PeerGatekeeper gatekeeper)
{
    private const string NotJoinedLog = "{command} from peer {peerId} dropped: the peer has not joined";
    private const string RejoinLog = "{command} from peer {peerId} dropped: the peer has already joined as {uid}";
    private const string NotValidLog = "{command} from peer {peerId} dropped: {handler} did not validate it";
    private const string DisconnectingLog = "{entry} dropped: the peer is being disconnected";
    private const string JoinRejectedLog = "{command} from peer {peerId} rejected by {handler}: {reason}";
    private const string JoinValidateFailedLog = "{handler} failed to validate {command}, the peer is rejected";
    private const string JoinProcessFailedLog = "{handler} failed to process {command}, the peer is rejected";
    private const string HostUidLog = "Peer {peerId} joins as {uid}, which is the host's own: rejected";
    private const string DisplacedLog = "Peer {newPeerId} joins as {uid}, displacing peer {oldPeerId}";
    private const string NotJoinedLeftLog = "Peer {peerId} disconnected before joining";
    private const string EntryFailedLog = "{entry} failed, the rest of the tick goes on";
    private const string RegisteredError = "The command handlers are already registered.";
    private const string NotRegisteredError = "The command handlers are not registered yet.";
    private const string NoInterfaceError = "{0} is a [CommandHandler] but implements no command handler interface.";
    private const string SecondHandlerError = "{0} and {1} both handle {2}.";
    private const string UnpairedJoinError =
        "There is a join handler without a peer disconnected handler, or the reverse: a joined peer must be able "
        + "to leave, and only a joined one can.";
    private const string UnknownEntryError = "{0} has no branch in Process.";
    private const string NoJoinHandlerError =
        "JoinRequestCommand from peer {0} passed the inbox, but there is no join handler: the whitelist is miswired.";
    private const string NoPlayerHandlerError =
        "{0} from peer {1} passed the inbox, but there is no player handler: the whitelist is miswired.";
    private const string JoinAsPlayerCommandError =
        "{0} handles JoinRequestCommand as a player command, but a joining peer has no player yet: "
        + "implement IJoinRequestHandler instead.";

    private record PlayerHandler(
        string Name,
        Func<string, Command, bool> Validate,
        Action<string, Command> Process);

    private static readonly MethodInfo WrapMethod =
        typeof(CommandDispatcher).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly ILogger _log = LogFactory.GetForStatic<CommandDispatcher>();

    private readonly Dictionary<Type, PlayerHandler> _playerHandlerByType = new();
    private IJoinRequestHandler _joinHandler;
    private IPeerDisconnectedHandler _disconnectedHandler;

    /// <summary>
    /// The commands a peer may send: those with a player handler, and <see cref="JoinRequestCommand"/> when there
    /// is a join handler. Everything else is rejected by <see cref="CommandInbox"/> before its body is read.
    /// </summary>
    public IReadOnlySet<Type> NetworkCommandTypes { get; private set; }

    public void Register(IEnumerable<object> handlers)
    {
        if (NetworkCommandTypes != null)
        {
            throw new InvalidOperationException(RegisteredError);
        }

        foreach (object handler in handlers)
        {
            bool found = false;
            foreach (Type implemented in handler.GetType().GetInterfaces())
            {
                found |= TryRegister(handler, implemented);
            }

            if (!found)
            {
                throw new InvalidOperationException(NoInterfaceError.FormatWith(handler.GetType().FullName));
            }
        }

        if ((_joinHandler == null) != (_disconnectedHandler == null))
        {
            throw new InvalidOperationException(UnpairedJoinError);
        }

        HashSet<Type> networkTypes = _playerHandlerByType.Keys.ToHashSet();
        if (_joinHandler != null)
        {
            networkTypes.Add(typeof(JoinRequestCommand));
        }
        NetworkCommandTypes = networkTypes;
    }

    public void ProcessAll()
    {
        if (NetworkCommandTypes == null)
        {
            throw new InvalidOperationException(NotRegisteredError);
        }

        foreach (CommandInbox.Entry entry in inbox.TakeAll())
        {
            // TakeAll has already emptied the inbox, so a throw must not cost the rest of the entries: a lost
            // PeerDisconnected would leave its peer bound. A facade has no rollback, so a throw may leave the model
            // half-changed: the log is the only signal of that
            try
            {
                Process(entry);
            }
            catch (Exception e)
            {
                _log.Error(e, EntryFailedLog, entry);
            }
        }
    }

    private void Process(CommandInbox.Entry entry)
    {
        // Its commands were sent before the disconnect reached it, so they are not its owner's any more
        if (entry is not CommandInbox.PeerDisconnected && gatekeeper.IsDisconnecting(entry.PeerId))
        {
            _log.Debug(DisconnectingLog, entry);
            return;
        }

        switch (entry)
        {
            case CommandInbox.PeerCommand { Command: JoinRequestCommand join }:
                ProcessJoin(entry.PeerId, join);
                break;
            case CommandInbox.PeerCommand peerCommand:
                ProcessFromPlayer(peerCommand.PeerId, peerCommand.Command);
                break;
            case CommandInbox.PeerDisconnected:
                ProcessDisconnected(entry.PeerId);
                break;
            default:
                throw new InvalidOperationException(UnknownEntryError.FormatWith(entry.GetType().Name));
        }
    }

    private bool TryRegister(object handler, Type implemented)
    {
        if (implemented == typeof(IJoinRequestHandler))
        {
            if (_joinHandler != null)
            {
                throw SecondHandler(_joinHandler.GetType().Name, handler, typeof(JoinRequestCommand));
            }
            _joinHandler = (IJoinRequestHandler) handler;
            return true;
        }
        if (implemented == typeof(IPeerDisconnectedHandler))
        {
            if (_disconnectedHandler != null)
            {
                string first = _disconnectedHandler.GetType().Name;
                throw SecondHandler(first, handler, typeof(CommandInbox.PeerDisconnected));
            }
            _disconnectedHandler = (IPeerDisconnectedHandler) handler;
            return true;
        }

        if (!implemented.IsGenericType || implemented.GetGenericTypeDefinition() != typeof(IPlayerCommandHandler<>))
        {
            return false;
        }

        Type commandType = implemented.GetGenericArguments()[0];
        // ProcessAll routes every JoinRequestCommand to the join handler, so this one would never be called
        if (commandType == typeof(JoinRequestCommand))
        {
            throw new InvalidOperationException(JoinAsPlayerCommandError.FormatWith(handler.GetType().FullName));
        }
        if (_playerHandlerByType.TryGetValue(commandType, out PlayerHandler existing))
        {
            throw SecondHandler(existing.Name, handler, commandType);
        }
        // Built once per handler through a generic method: no reflection per command, and the handler's own
        // exception is not wrapped into a TargetInvocationException
        var wrapped = (PlayerHandler) WrapMethod.MakeGenericMethod(commandType).Invoke(null, [handler])!;
        _playerHandlerByType.Add(commandType, wrapped);
        return true;
    }

    private void ProcessJoin(int peerId, JoinRequestCommand command)
    {
        string name = command.GetType().Name;
        if (peers.TryGetUid(peerId, out string joinedUid))
        {
            _log.Warning(RejoinLog, name, peerId, joinedUid);
            return;
        }
        // The inbox lets the join through only when there is a join handler, so this is a wiring error, not a peer's
        if (_joinHandler == null)
        {
            throw new InvalidOperationException(NoJoinHandlerError.FormatWith(peerId));
        }

        string handlerName = _joinHandler.GetType().Name;
        JoinRejectReason reason;
        bool valid;
        try
        {
            valid = _joinHandler.Validate(command, out reason);
            if (!valid)
            {
                _log.Warning(JoinRejectedLog, name, peerId, handlerName, reason);
            }
        }
        // Caught here rather than in ProcessAll: the peer still has to be rejected
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
            _joinHandler.Process(command);
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

    private void ProcessDisconnected(int peerId)
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
            _disconnectedHandler.Process(uid);
        }
        finally
        {
            outbox.RemovePeer(peerId);
            peers.Unbind(peerId);
        }
    }

    private void ProcessFromPlayer(int peerId, Command command)
    {
        string name = command.GetType().Name;
        if (!peers.TryGetUid(peerId, out string uid))
        {
            _log.Warning(NotJoinedLog, name, peerId);
            return;
        }

        // The inbox lets through only the commands with a handler, so this is a wiring error, not a peer's
        if (!_playerHandlerByType.TryGetValue(command.GetType(), out PlayerHandler handler))
        {
            throw new InvalidOperationException(NoPlayerHandlerError.FormatWith(name, peerId));
        }

        if (!handler.Validate(uid, command))
        {
            _log.Warning(NotValidLog, name, peerId, handler.Name);
            return;
        }

        handler.Process(uid, command);
    }

    private static InvalidOperationException SecondHandler(string first, object second, Type command) =>
        new(SecondHandlerError.FormatWith(first, second.GetType().Name, command.Name));

    private static PlayerHandler Wrap<T>(IPlayerCommandHandler<T> handler) where T : Command =>
        new(handler.GetType().Name,
            (sender, command) => handler.Validate(sender, (T) command),
            (sender, command) => handler.Process(sender, (T) command));
}
