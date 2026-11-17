using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Infra.ClientNetwork;

/// <summary>
/// The only sender of commands in the World. <c>JoinRequestCommand</c> does not go through it: <c>Game</c> sends the
/// join itself, before a remote client has a World.
/// </summary>
[ClientNetwork]
public class PlayerCommandSender(NetMessageCodec codec, IServerConnection serverConnection)
{
    public void Send<TCommand>(TCommand command) where TCommand : Command =>
        serverConnection.Send(codec.Encode(command));
}
