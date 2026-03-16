using NeonWarfare.Scenes.World.Data.PersistenceData.Player;
using Humanizer;

namespace NeonWarfare.Scenes.World.Service.Command.Impl;

public class ControlAdminsCommand : ICommandProcessor
{
    private const string AddAdminParam = "add";
    private const string RemoveAdminParam = "remove";
    
    private const string AddAdminSuccessfullyMessage = "Successfully add admin '{0}'.";
    private const string RemoveAdminSuccessfullyMessage = "Successfully remove admin '{0}'.";
    
    private const string RequireParamErrorMessage = "Command '{0}' require param: '" + AddAdminParam + "' or '" + RemoveAdminParam + "'.";
    private const string RequireNickErrorMessage = "Command '{0}' require nick after param '" + AddAdminParam + "' or '" + RemoveAdminParam + "'.";
    private const string PlayerNotFoundErrorMessage = "Player '{0}' not found.";
    private const string AlwaysAdminErrorMessage = "Player '{0}' always admin.";
    private const string AlwaysNotAdminErrorMessage = "Player '{0}' always not admin.";
    
    public string GetCommand() => "admin";
    public string GetDescription() => "Control list of admins. Format: admin {add|remove} <nickname>";
    public bool IsRequiringAdmin() => true;

    public void ProcessCommand(int senderId, string command, World world)
    {
        string[] paramsArray = this.ParseParams(command);
        if (paramsArray.Length == 0)
        {
            SendMessage(RequireParamErrorMessage.FormatWith(GetCommand()), senderId, world);
            return;
        }
        if (paramsArray.Length == 1)
        {
            SendMessage(RequireNickErrorMessage.FormatWith(GetCommand()), senderId, world);
            return;
        }
        
        string action = paramsArray[0].ToLower();
        string nick = paramsArray[1];

        if (action != AddAdminParam && action != RemoveAdminParam)
        {
            SendMessage(RequireParamErrorMessage.FormatWith(GetCommand()), senderId, world);
            return;
        }
        if (!world.PersistenceData.Players.PlayerByNick.ContainsKey(nick))
        {
            SendMessage(PlayerNotFoundErrorMessage.FormatWith(nick), senderId, world);
            return;
        }

        switch (action)
        {
            case AddAdminParam: AddAdmin(nick, senderId, world); break;
            case RemoveAdminParam: RemoveAdmin(nick, senderId, world); break;
        }
    }

    private void SendMessage(string message, int senderId, World world)
    {
        world.ChatService.TrySendNewMessage(message, senderId);
    }

    private void AddAdmin(string nick, int senderId, World world)
    {
        PlayerData playerData = world.PersistenceData.Players.PlayerByNick[nick];
        if (playerData.IsAdmin)
        {
            SendMessage(AlwaysAdminErrorMessage.FormatWith(nick), senderId, world);
            return;
        }
        
        playerData.IsAdmin = true;
        SendMessage(AddAdminSuccessfullyMessage.FormatWith(nick), senderId, world);
    }
    
    private void RemoveAdmin(string nick, int senderId, World world)
    {
        PlayerData playerData = world.PersistenceData.Players.PlayerByNick[nick];
        if (!playerData.IsAdmin)
        {
            SendMessage(AlwaysNotAdminErrorMessage.FormatWith(nick), senderId, world);
            return;
        }
        
        playerData.IsAdmin = false;
        SendMessage(RemoveAdminSuccessfullyMessage.FormatWith(nick), senderId, world);
    }
}