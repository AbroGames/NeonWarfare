namespace NeonWarfare.Scenes.World.CommandHandlers;

// In difference with IPlayerCommandHandler, here no validate: Server console input is trusted.
public interface IConsoleCommandHandler<TCommand>
{
    void Process(TCommand command);
}
