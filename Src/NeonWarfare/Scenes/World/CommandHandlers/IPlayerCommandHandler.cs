using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.CommandHandlers;

public interface IPlayerCommandHandler<TCommand>
{
    bool Validate(PlayerModel sender, TCommand command);
    void Process(PlayerModel sender, TCommand command);
}