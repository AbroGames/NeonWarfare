using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace NeonWarfare.Scenes.World.Service.Command.Impl;

public class HelpCommand : ICommandProcessor
{
    private const string PlayerCommandsMessage = "\nPlayer commands:\n{0}";
    private const string AdminCommandsMessage = "\nAdmin commands:\n{0}";
    private const string CommandFormat = "   '/{0}' -> {1}";
    
    public string GetCommand() => "help";
    public string GetDescription() => "Show list of available commands.";
    public bool IsRequiringAdmin() => false;
    
    public void ProcessCommand(int senderId, string command, World world)
    {
        IEnumerable<ICommandProcessor> playerCommands = world.CommandService.CommandProcessorByCommand.Values
            .Where(processor => !processor.IsRequiringAdmin());
        string message = PlayerCommandsMessage.FormatWith(GetListOfCommands(playerCommands));

        if (world.FacadeService.IsAdmin(senderId))
        {
            IEnumerable<ICommandProcessor> adminCommands = world.CommandService.CommandProcessorByCommand.Values
                .Where(processor => processor.IsRequiringAdmin());
            message += AdminCommandsMessage.FormatWith(GetListOfCommands(adminCommands));
        }
        
        world.ChatService.TrySendNewMessage(message, senderId);
    }

    private string GetListOfCommands(IEnumerable<ICommandProcessor> commands)
    {
        IEnumerable<string> commandInfos = commands
            .Select(command => CommandFormat.FormatWith(command.GetCommand(), command.GetDescription()));
        return string.Join("\n", commandInfos);
    }
}