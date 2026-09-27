using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Simulations;

[Simulation(Facade = true)]
public class ChatSimulationFacade(ChatSimulation chatSimulation)
{
    public void SendMessage(PlayerModel sender, string text)
    {
        if (text.StartsWith('/'))
        {
            //ChatCommandSimulation.ProcessChatCommand(sender, text.Substring(1))
            return;
        }
        //chatSimulation.SendMessageAsPlayerToAll(sender, text)
    }
}
