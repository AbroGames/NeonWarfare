using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Worlds.Infra.Client;

/// <summary>
/// The only sender of commands in the World. <c>JoinRequestCommand</c> does not go through it: the transport of
/// <c>Game</c> sends the join itself, since a remote client sends it before it has a World.
/// </summary>
[Client]
public class PlayerCommandSender(NetMessageCodec codec, IServerConnection serverConnection)
{
    public void Send<TCommand>(TCommand command) where TCommand : Command =>
        serverConnection.Send(codec.Encode(command));
}
