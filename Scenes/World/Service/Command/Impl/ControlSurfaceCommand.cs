using Humanizer;

namespace NeonWarfare.Scenes.World.Service.Command.Impl;

public class ControlSurfaceCommand : ICommandProcessor
{
    private const string SafeSurfaceParam = "safe";
    private const string BattleSurfaceParam = "battle";
    
    private const string ChangedSuccessfullyMessage = "Successfully changed surface to '{0}'.";
    private const string RequireParamErrorMessage = "Command '{0}' require param: '" + SafeSurfaceParam + "' or '" + BattleSurfaceParam + "'.";
    
    public string GetCommand() => "surface";
    public string GetDescription() => "Change current surface. Format: surface {safe|battle}";
    public bool IsRequiringAdmin() => true;

    public void ProcessCommand(int senderId, string command, World world)
    {
        string[] paramsArray = this.ParseParams(command);
        if (paramsArray.Length == 0)
        {
            SendMessage(RequireParamErrorMessage.FormatWith(GetCommand()), senderId, world);
            return;
        }
        
        string surface = paramsArray[0].ToLower();

        if (surface != SafeSurfaceParam && surface != BattleSurfaceParam)
        {
            SendMessage(RequireParamErrorMessage.FormatWith(GetCommand()), senderId, world);
            return;
        }

        switch (surface)
        {
            case SafeSurfaceParam: 
                world.Tree.SetSafeSurface();
                SendMessage(ChangedSuccessfullyMessage.FormatWith(surface), senderId, world);
                break;
            case BattleSurfaceParam: 
                world.Tree.SetBattleSurface();
                SendMessage(ChangedSuccessfullyMessage.FormatWith(surface), senderId, world);
                break;
        }
    }

    private void SendMessage(string message, int senderId, World world)
    {
        world.ChatService.TrySendNewMessage(message, senderId);
    }
}