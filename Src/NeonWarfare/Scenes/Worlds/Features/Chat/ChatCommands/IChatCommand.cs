namespace NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;

// Implemented by a [SimulationFacade] named *ChatCommandSimulationFacade: a command may change the state
// through other facades. A command implements IListedChatCommand; only /help implements this one alone, since it
// takes every listed command and would otherwise take itself
public interface IChatCommand
{
    // Lower case, no whitespace: what follows the '/'
    string Name { get; }
    string Description { get; }
    bool RequiresAdmin { get; }

    void Execute(string senderUid, string arguments);
}
