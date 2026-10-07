using NeonWarfare.Scenes.World.Commands;

namespace NeonWarfare.Scenes.World.CommandHandlers;

// In difference with IPlayerCommandHandler, here no validate: the dedicated window input is trusted.
public interface IDedicatedWindowCommandHandler<TCommand> where TCommand : Command
{
    void Process(TCommand command);
}
