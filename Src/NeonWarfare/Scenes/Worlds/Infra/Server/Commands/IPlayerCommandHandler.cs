using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;


namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

public interface IPlayerCommandHandler<TCommand> : ICommandHandler where TCommand : Command
{
    bool Validate(PlayerUid senderUid, TCommand command);
    void Process(PlayerUid senderUid, TCommand command);
}
