using NeonWarfare.Scenes.Game.ServerGame.ServerCommandsService;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
    public CommandsService CommandsService { get; set; } = new();
    public void BroadcastMessage(string messageText)
    {
        Network.SendToAll(new ChatNetworking.SC_SendMessagePacket(messageText, -1));
    }

    public void SendMessageTo(long peerId, string messageText)
    {
        Network.SendToClient(peerId, new ChatNetworking.SC_SendMessagePacket(messageText, -1));
    }
}