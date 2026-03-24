using System.Linq;
using Humanizer;

namespace NeonWarfare.Scenes.World.Service.Command.Impl;

public class GetPlayersCommand : ICommandProcessor
{
    private const string PlayersMessage = "Players online: {0}.\nPlayers offline: {1}.";
    private const string NotFoundText = "not found";
    
    public string GetCommand() => "players";
    public string GetDescription() => "Show list of players.";
    public bool IsRequiringAdmin() => false;

    public void ProcessCommand(int senderId, string command, World world)
    {
        string playersOnline = string.Join(", ", world.FacadeService.GetOnlinePlayers()
            .Select(playerData => playerData.Nick)
            .DefaultIfEmpty(NotFoundText));
        
        string playersOffline = string.Join(", ", world.FacadeService.GetOfflinePlayers()
            .Select(playerData => playerData.Nick)
            .DefaultIfEmpty(NotFoundText));
        
        string message = PlayersMessage.FormatWith(playersOnline, playersOffline);
        world.ChatService.TrySendNewMessage(message, senderId);
    }
}