using System;

namespace NeonWarfare.Scenes.World.Simulations.ChatCommands;

// Implemented by a [SimulationFacade] named *ChatCommandSimulationFacade: a command may change the state
// through other facades, and the composition root passes every created one to ChatSimulationFacade.Register
public interface IChatCommand
{
    // Lower case, no whitespace: what follows the '/'
    string Name { get; }
    string Description { get; }
    bool RequiresAdmin { get; }

    // reply goes back to whoever ran the command: the player alone, or the dedicated window alone
    void Execute(bool isAdmin, Action<string> reply, string arguments);
}
