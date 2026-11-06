using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;


namespace NeonWarfare.Scenes.World.Infra.ServerNetwork;

public interface IPlayerCommandHandler<TCommand> where TCommand : Command
{
    bool Validate(PlayerModel sender, TCommand command);
    void Process(PlayerModel sender, TCommand command);
}