using System.Linq;
using Humanizer;

namespace NeonWarfare.Scenes.World.Service.Command.Impl;

public class GetAdminsCommand : ICommandProcessor
{
    private const string AdminsMessage = "Admins online: {0}.\nAdmins offline: {1}.";
    private const string NotFoundText = "not found";
    
    public string GetCommand() => "admins";
    public string GetDescription() => "Show list of admins.";
    public bool IsRequiringAdmin() => false;

    public void ProcessCommand(int senderId, string command, World world)
    {
        string adminsOnline = string.Join(", ", world.FacadeService.GetOnlinePlayers()
            .Where(playerData => playerData.IsAdmin)
            .Select(playerData => playerData.Nick)
            .DefaultIfEmpty(NotFoundText));
        
        string adminsOffline = string.Join(", ", world.FacadeService.GetOfflinePlayers()
            .Where(playerData => playerData.IsAdmin)
            .Select(playerData => playerData.Nick)
            .DefaultIfEmpty(NotFoundText));
        
        string message = AdminsMessage.FormatWith(adminsOnline, adminsOffline);
        world.ChatService.TrySendNewMessage(message, senderId);
    }
}