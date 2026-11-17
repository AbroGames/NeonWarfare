using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;

/// <summary>
/// Every network handler of the world, and the command whitelist built from them. The handlers are collected once,
/// by the composition root, since MS.DI cannot inject "every <see cref="IPlayerCommandHandler{TCommand}"/>".
/// </summary>
[ServerNetwork]
public class CommandHandlerRegistry
{
    private const string RegisteredError = "The command handlers are already registered.";
    private const string NotRegisteredError = "The command handlers are not registered yet.";
    private const string NoInterfaceError = "{0} is a [CommandHandler] but implements no command handler interface.";
    private const string SecondHandlerError = "{0} and {1} both handle {2}.";
    private const string UnpairedJoinError =
        "There is a join handler without a peer disconnected handler, or the reverse: a joined peer must be able "
        + "to leave, and only a joined one can.";
    private const string JoinAsPlayerCommandError =
        "{0} handles JoinRequestCommand as a player command, but a joining peer has no player yet: "
        + "implement IJoinRequestHandler instead.";

    public record PlayerHandler(
        string Name,
        Func<string, Command, bool> Validate,
        Action<string, Command> Process);

    private static readonly MethodInfo WrapMethod =
        typeof(CommandHandlerRegistry).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly Dictionary<Type, PlayerHandler> _playerHandlerByType = new();
    private IReadOnlySet<Type> _networkCommandTypes;

    /// <summary>
    /// <c>null</c> when the world has none; then <see cref="DisconnectedHandler"/> is <c>null</c> too.
    /// </summary>
    public IJoinRequestHandler JoinHandler { get; private set; }

    public IPeerDisconnectedHandler DisconnectedHandler { get; private set; }

    /// <summary>
    /// The commands a peer may send: those with a player handler, and <see cref="JoinRequestCommand"/> when there
    /// is a join handler. Everything else is rejected by <see cref="CommandInbox"/> before its body is read.
    /// </summary>
    public IReadOnlySet<Type> NetworkCommandTypes =>
        _networkCommandTypes ?? throw new InvalidOperationException(NotRegisteredError);

    public bool IsRegistered => _networkCommandTypes != null;

    public void Register(IEnumerable<object> handlers)
    {
        if (IsRegistered)
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

        if ((JoinHandler == null) != (DisconnectedHandler == null))
        {
            throw new InvalidOperationException(UnpairedJoinError);
        }

        HashSet<Type> networkTypes = _playerHandlerByType.Keys.ToHashSet();
        if (JoinHandler != null)
        {
            networkTypes.Add(typeof(JoinRequestCommand));
        }
        _networkCommandTypes = networkTypes;
    }

    public bool TryGetPlayerHandler(Type commandType, out PlayerHandler handler) =>
        _playerHandlerByType.TryGetValue(commandType, out handler);

    private bool TryRegister(object handler, Type implemented)
    {
        if (implemented == typeof(IJoinRequestHandler))
        {
            if (JoinHandler != null)
            {
                throw SecondHandler(JoinHandler.GetType().Name, handler, typeof(JoinRequestCommand));
            }
            JoinHandler = (IJoinRequestHandler) handler;
            return true;
        }
        if (implemented == typeof(IPeerDisconnectedHandler))
        {
            if (DisconnectedHandler != null)
            {
                string first = DisconnectedHandler.GetType().Name;
                throw SecondHandler(first, handler, typeof(CommandInbox.PeerDisconnected));
            }
            DisconnectedHandler = (IPeerDisconnectedHandler) handler;
            return true;
        }

        if (!implemented.IsGenericType || implemented.GetGenericTypeDefinition() != typeof(IPlayerCommandHandler<>))
        {
            return false;
        }

        Type commandType = implemented.GetGenericArguments()[0];
        // CommandDispatcher routes every JoinRequestCommand to the join handler, so this one would never be called
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

    private static InvalidOperationException SecondHandler(string first, object second, Type command) =>
        new(SecondHandlerError.FormatWith(first, second.GetType().Name, command.Name));

    private static PlayerHandler Wrap<T>(IPlayerCommandHandler<T> handler) where T : Command =>
        new(handler.GetType().Name,
            (sender, command) => handler.Validate(sender, (T) command),
            (sender, command) => handler.Process(sender, (T) command));
}
