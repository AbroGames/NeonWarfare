using Humanizer;

namespace NeonWarfare.Scenes.World.Service.Command.Impl;

public class NotFoundCommand : ICommandProcessor
{

    private const string CommandNotFoundMessage = "Command '{0}' not found. Use '/help' for list of available commands.";
    
    public string GetCommand() => "";
    public string GetDescription() => "Default handler for unknown commands.";
    public bool IsRequiringAdmin() => false;
    
    public void ProcessCommand(int senderId, string command, World world)
    {
        string commandWithoutParams = command.Split(' ')[0];
        string message = CommandNotFoundMessage.FormatWith(commandWithoutParams);
        world.ChatService.TrySendNewMessage(message, senderId);
    }
}