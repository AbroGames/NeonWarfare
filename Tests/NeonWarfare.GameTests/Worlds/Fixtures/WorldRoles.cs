using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// A test drives a World through every role at once, as no single caller of the game does: each entry point goes
/// through the role interface <see cref="World"/> implements it for.
/// </summary>
public static class WorldRoles
{
    public static T Get<T>(this World world) where T : class => ((World.IReader) world).Get<T>();

    public static void Send<TCommand>(this World world, TCommand command) where TCommand : Command =>
        ((World.ICommandSender) world).Send(command);

    public static void StartHandshake(this World world, int peerId) =>
        ((World.IServerEndpoint) world).StartHandshake(peerId);

    public static void QueueDisconnection(this World world, int peerId) =>
        ((World.IServerEndpoint) world).QueueDisconnection(peerId);

    public static void ReceiveFromClient(this World world, int peerId, ReadOnlyMemory<byte> packet) =>
        ((World.IServerEndpoint) world).ReceiveFromClient(peerId, packet);

    public static void ReceiveOwnEvents(this World world, ReadOnlyMemory<byte> events) =>
        ((World.IHostEndpoint) world).ReceiveOwnEvents(events);

    public static void ReceiveTickUpdate(
        this World world, long tick, ReadOnlyMemory<byte> state, ReadOnlyMemory<byte> events) =>
        ((World.IClientEndpoint) world).ReceiveTickUpdate(tick, state, events);
}
