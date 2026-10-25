using NeonWarfare.Scenes.World.Features.Players;


namespace NeonWarfare.Scenes.World.Features.Chat.ChatCommands;

// Implemented by a [SimulationFacade] named *ChatCommandSimulationFacade: a command may change the state
// through other facades, and the composition root passes every created one to ChatSimulationFacade.Register
public interface IChatCommand
{
    // Lower case, no whitespace: what follows the '/'
    string Name { get; }
    string Description { get; }
    bool RequiresAdmin { get; }

    void Execute(PlayerModel sender, string arguments);
}
