using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

/// <summary>The player command handlers of the world, and the command whitelist built from them.</summary>
[Server]
public class CommandHandlerRegistry
{
    public record PlayerHandler(
        string Name,
        Func<PlayerUid, Command, bool> Validate,
        Action<PlayerUid, Command> Process);

    private static readonly MethodInfo WrapMethod =
        typeof(CommandHandlerRegistry).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly Dictionary<Type, PlayerHandler> _playerHandlerByType = new();

    /// <summary>
    /// The commands a peer may send: those with a player handler, and <see cref="JoinRequestCommand"/>.
    /// <see cref="CommandInbox"/> drops any other type before its body is read.
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
                throw new InvalidOperationException(
                    $"{handler.GetType().FullName} is a [CommandHandler] but implements no command handler interface.");
            }
        }

        NetworkCommandTypes = _playerHandlerByType.Keys.Append(typeof(JoinRequestCommand)).ToHashSet();
    }

    public PlayerHandler GetPlayerHandler(Type commandType) => _playerHandlerByType[commandType];

    private bool TryAdd(object handler, Type implemented)
    {
        if (!implemented.IsGenericType || implemented.GetGenericTypeDefinition() != typeof(IPlayerCommandHandler<>))
        {
            return false;
        }

        Type commandType = implemented.GetGenericArguments()[0];
        if (_playerHandlerByType.TryGetValue(commandType, out PlayerHandler existing))
        {
            throw new InvalidOperationException(
                $"{existing.Name} and {handler.GetType().Name} both handle {commandType.Name}.");
        }
        // The wrapper is built once per handler through a generic method, so no command goes through reflection.
        // The handler's own exception is then not wrapped into a TargetInvocationException.
        var wrapped = (PlayerHandler) WrapMethod.MakeGenericMethod(commandType).Invoke(null, [handler])!;
        _playerHandlerByType.Add(commandType, wrapped);
        return true;
    }

    private static PlayerHandler Wrap<T>(IPlayerCommandHandler<T> handler) where T : Command =>
        new(handler.GetType().Name,
            (sender, command) => handler.Validate(sender, (T) command),
            (sender, command) => handler.Process(sender, (T) command));
}
