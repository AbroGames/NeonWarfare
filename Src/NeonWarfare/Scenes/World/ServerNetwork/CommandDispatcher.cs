using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.CommandHandlers;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using Serilog;

namespace NeonWarfare.Scenes.World.ServerNetwork;

/// <summary>
/// Drains <see cref="CommandInbox"/> in the server tick and hands every entry to the handler of its sender kind.
/// The handlers are collected once, by the composition root, since MS.DI cannot inject "every
/// <see cref="IPlayerCommandHandler{TCommand}"/>". "Joined or not" and "which player is this peer" are decided here,
/// at tick time rather than on arrival: a join and a chat command of one peer in one tick must both pass, in order.
/// </summary>
[ServerNetwork]
public class CommandDispatcher(CommandInbox inbox, PeerUidMap peers, PersistenceModel persistence)
{
    private const string NotJoinedLog = "{command} from peer {peerId} dropped: the peer has not joined";
    private const string NoPlayerLog =
        "{command} from peer {peerId} dropped: the peer is bound to {uid}, but there is no player with this uid";
    private const string RejoinLog = "{command} from peer {peerId} dropped: the peer has already joined as {uid}";
    private const string NoPlayerHandlerLog = "{command} from peer {peerId} dropped: no player handler";
    private const string NoDedicatedWindowHandlerLog =
        "{command} from the dedicated window dropped: no dedicated-window handler";
    private const string NotValidLog = "{command} from peer {peerId} dropped: {handler} did not validate it";
    private const string JoinRejectedLog = "{command} from peer {peerId} rejected by {handler}: {reason}";
    private const string JoinValidateFailedLog = "{handler} failed to validate {command}, the peer is rejected";
    private const string EntryFailedLog = "{entry} failed, the rest of the tick goes on";
    private const string ValidateFailedReason = "internal server error";
    private const string RegisteredError = "The command handlers are already registered.";
    private const string NotRegisteredError = "The command handlers are not registered yet.";
    private const string NoInterfaceError = "{0} is a [CommandHandler] but implements no command handler interface.";
    private const string SecondHandlerError = "{0} and {1} both handle {2} from {3}.";
    private const string UnknownEntryError = "{0} has no branch in ProcessAll.";
    private const string NoJoinHandlerError =
        "JoinRequestCommand from peer {0} passed the inbox, but there is no join handler: the whitelist is miswired.";
    private const string JoinAsPlayerCommandError =
        "{0} handles JoinRequestCommand as a player command, but a joining peer has no player yet: "
        + "implement IJoinRequestHandler instead.";
    private const string PlayerSenderKind = "a player";
    private const string PeerSenderKind = "a peer";
    private const string DedicatedWindowSenderKind = "the dedicated window";

    private record PlayerHandler(
        string Name,
        Func<PlayerModel, Command, bool> Validate,
        Action<PlayerModel, Command> Process);
    private record DedicatedWindowHandler(
        string Name,
        Action<Command> Process);

    private static readonly MethodInfo WrapPlayerMethod =
        typeof(CommandDispatcher).GetMethod(nameof(WrapPlayer), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo WrapDedicatedWindowMethod =
        typeof(CommandDispatcher).GetMethod(nameof(WrapDedicatedWindow), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly ILogger _log = LogFactory.GetForStatic<CommandDispatcher>();

    private readonly Dictionary<Type, PlayerHandler> _playerHandlerByType = new();
    private readonly Dictionary<Type, DedicatedWindowHandler> _dedicatedWindowHandlerByType = new();
    private IJoinRequestHandler _joinHandler;

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
        switch (entry)
        {
            case CommandInbox.FromPeer { Command: JoinRequestCommand join } fromPeer:
                ProcessJoin(fromPeer.PeerId, join);
                break;
            case CommandInbox.FromPeer fromPeer:
                ProcessFromPlayer(fromPeer.PeerId, fromPeer.Command);
                break;
            case CommandInbox.FromDedicatedWindow fromWindow:
                ProcessFromDedicatedWindow(fromWindow.Command);
                break;
            case CommandInbox.PeerDisconnected:
                //TODO 015 Leave
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
                throw SecondHandler(_joinHandler.GetType().Name, handler, typeof(JoinRequestCommand), PeerSenderKind);
            }
            _joinHandler = (IJoinRequestHandler) handler;
            return true;
        }

        if (!implemented.IsGenericType)
        {
            return false;
        }

        Type definition = implemented.GetGenericTypeDefinition();
        Type commandType = implemented.GetGenericArguments()[0];
        if (definition == typeof(IPlayerCommandHandler<>))
        {
            // ProcessAll routes every JoinRequestCommand to the join handler, so this one would never be called
            if (commandType == typeof(JoinRequestCommand))
            {
                throw new InvalidOperationException(JoinAsPlayerCommandError.FormatWith(handler.GetType().FullName));
            }
            if (_playerHandlerByType.TryGetValue(commandType, out PlayerHandler existing))
            {
                throw SecondHandler(existing.Name, handler, commandType, PlayerSenderKind);
            }
            _playerHandlerByType.Add(commandType, (PlayerHandler) Wrap(WrapPlayerMethod, commandType, handler));
            return true;
        }

        if (definition == typeof(IDedicatedWindowCommandHandler<>))
        {
            if (_dedicatedWindowHandlerByType.TryGetValue(commandType, out DedicatedWindowHandler existing))
            {
                throw SecondHandler(existing.Name, handler, commandType, DedicatedWindowSenderKind);
            }
            _dedicatedWindowHandlerByType.Add(
                commandType,
                (DedicatedWindowHandler) Wrap(WrapDedicatedWindowMethod, commandType, handler));
            return true;
        }

        return false;
    }

    private void ProcessJoin(int peerId, JoinRequestCommand command)
    {
        string name = command.GetType().Name;
        if (peers.TryGetUid(peerId, out string uid))
        {
            _log.Warning(RejoinLog, name, peerId, uid);
            return;
        }
        // The inbox lets the join through only when there is a join handler, so this is a wiring error, not a peer's
        if (_joinHandler == null)
        {
            throw new InvalidOperationException(NoJoinHandlerError.FormatWith(peerId));
        }

        string handlerName = _joinHandler.GetType().Name;
        string reason;
        bool valid;
        try
        {
            valid = _joinHandler.Validate(peerId, command, out reason);
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
            reason = ValidateFailedReason;
        }

        if (!valid)
        {
            //TODO 015 JoinRejected(reason) and disconnect the peer
            return;
        }

        _joinHandler.Process(peerId, command);
    }

    private void ProcessFromPlayer(int peerId, Command command)
    {
        string name = command.GetType().Name;
        if (!peers.TryGetUid(peerId, out string uid))
        {
            _log.Warning(NotJoinedLog, name, peerId);
            return;
        }
        // A bound peer always has a player: this is a broken join or leave, not a peer's misbehaviour
        if (!persistence.PlayerByUid.TryGetValue(uid, out PlayerModel sender))
        {
            _log.Error(NoPlayerLog, name, peerId, uid);
            return;
        }

        if (!_playerHandlerByType.TryGetValue(command.GetType(), out PlayerHandler handler))
        {
            _log.Warning(NoPlayerHandlerLog, name, peerId);
            return;
        }

        if (!handler.Validate(sender, command))
        {
            _log.Warning(NotValidLog, name, peerId, handler.Name);
            return;
        }

        handler.Process(sender, command);
    }

    private void ProcessFromDedicatedWindow(Command command)
    {
        string name = command.GetType().Name;
        if (!_dedicatedWindowHandlerByType.TryGetValue(command.GetType(), out DedicatedWindowHandler handler))
        {
            _log.Warning(NoDedicatedWindowHandlerLog, name);
            return;
        }

        handler.Process(command);
    }

    private static InvalidOperationException SecondHandler(string first, object second, Type command, string kind) =>
        new(SecondHandlerError.FormatWith(first, second.GetType().Name, command.Name, kind));

    // Built once per handler through a generic method: no reflection per command, and the handler's own exception
    // is not wrapped into a TargetInvocationException
    private static object Wrap(MethodInfo wrap, Type commandType, object handler) =>
        wrap.MakeGenericMethod(commandType).Invoke(null, [handler])!;

    private static PlayerHandler WrapPlayer<T>(IPlayerCommandHandler<T> handler) where T : Command =>
        new(handler.GetType().Name,
            (sender, command) => handler.Validate(sender, (T) command),
            (sender, command) => handler.Process(sender, (T) command));

    private static DedicatedWindowHandler WrapDedicatedWindow<T>(IDedicatedWindowCommandHandler<T> handler)
        where T : Command =>
        new(handler.GetType().Name, command => handler.Process((T) command));
}
