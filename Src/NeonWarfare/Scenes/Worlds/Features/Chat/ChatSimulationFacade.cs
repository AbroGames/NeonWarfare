using NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[SimulationFacade]
public class ChatSimulationFacade(
    ChatSimulation chatSimulation, ChatCommandRunnerSimulationFacade chatCommandRunnerSimulationFacade)
{
    public void HandleChatInput(PlayerUid senderUid, string text)
    {
        if (text.StartsWith('/'))
        {
            chatCommandRunnerSimulationFacade.Run(senderUid, text[1..]);
            return;
        }
        chatSimulation.SendMessageAsPlayerToAll(senderUid, text);
    }
}
