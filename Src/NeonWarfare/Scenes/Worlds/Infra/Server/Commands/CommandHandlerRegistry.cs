using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

/// <summary>Every network handler of the world, and the command whitelist built from them.</summary>
[Server]
public class CommandHandlerRegistry
{
    private const string NoInterfaceError = "{0} is a [CommandHandler] but implements no command handler interface.";
    private const string SecondHandlerError = "{0} and {1} both handle {2}.";
    private const string JoinAsPlayerCommandError =
        "{0} handles JoinRequestCommand as a player command, but a joining peer has no player yet: "
        + "implement IPeerSessionHandler instead.";

    public record PlayerHandler(
        string Name,
        Func<string, Command, bool> Validate,
        Action<string, Command> Process);

    private static readonly MethodInfo WrapMethod =
        typeof(CommandHandlerRegistry).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly Dictionary<Type, PlayerHandler> _playerHandlerByType = new();

    /// <summary><c>null</c> when the world has none.</summary>
    public IPeerSessionHandler SessionHandler { get; private set; }

    /// <summary>
    /// The commands a peer may send: those with a player handler, and <see cref="JoinRequestCommand"/> when there
    /// is a session handler. Everything else is rejected by <see cref="CommandInbox"/> before its body is read.
    /// </summary>
    public IReadOnlySet<Type> NetworkCommandTypes { get; }

    public CommandHandlerRegistry(IEnumerable<ICommandHandler> handlers)
    {
        foreach (ICommandHandler handler in handlers)
        {
            bool found = false;
            foreach (Type implemented in handler.GetType().GetInterfaces())
            {
                found |= TryAdd(handler, implemented);
            }

            if (!found)
            {
                throw new InvalidOperationException(NoInterfaceError.FormatWith(handler.GetType().FullName));
            }
        }

        HashSet<Type> networkTypes = _playerHandlerByType.Keys.ToHashSet();
        if (SessionHandler != null)
        {
            networkTypes.Add(typeof(JoinRequestCommand));
        }
        NetworkCommandTypes = networkTypes;
    }

    public bool TryGetPlayerHandler(Type commandType, out PlayerHandler handler) =>
        _playerHandlerByType.TryGetValue(commandType, out handler);

    private bool TryAdd(object handler, Type implemented)
    {
        if (implemented == typeof(IPeerSessionHandler))
        {
            if (SessionHandler != null)
            {
                throw SecondHandler(SessionHandler.GetType().Name, handler, typeof(JoinRequestCommand));
            }
            SessionHandler = (IPeerSessionHandler) handler;
            return true;
        }

        if (!implemented.IsGenericType || implemented.GetGenericTypeDefinition() != typeof(IPlayerCommandHandler<>))
        {
            return false;
        }

        Type commandType = implemented.GetGenericArguments()[0];
        // CommandDispatcher routes every JoinRequestCommand to the session handler, so this one would never be called
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
