using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.CommandHandlers;

public interface IPlayerCommandHandler<TCommand> where TCommand : Command
{
    bool Validate(PlayerModel sender, TCommand command);
    void Process(PlayerModel sender, TCommand command);
}