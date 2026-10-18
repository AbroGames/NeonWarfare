using NeonWarfare.Scenes.World.Commands;

namespace NeonWarfare.Scenes.World.CommandHandlers;

public interface IDedicatedWindowCommandHandler<TCommand> where TCommand : Command
{
    bool Validate(TCommand command);
    void Process(TCommand command);
}
