using System.Collections.Generic;
using System.Linq;
using Humanizer;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Features.Chat.Commands;

[SimulationFacade]
public class HelpChatCommandSimulationFacade(ChatSimulationFacade chatFacade, ChatSimulation chatSimulation)
    : IChatCommand
{
    private const string PlayerCommandsMessage = "\nPlayer commands:\n{0}";
    private const string AdminCommandsMessage = "\nAdmin commands:\n{0}";
    private const string CommandFormat = "   '/{0}' -> {1}";

    public string Name => "help";
    public string Description => "Show list of available commands.";
    public bool RequiresAdmin => false;

    public void Execute(PlayerModel sender, string arguments)
    {
        string message = PlayerCommandsMessage.FormatWith(
            ListOf(chatFacade.Commands.Where(command => !command.RequiresAdmin)));
        if (sender.IsAdmin)
        {
            message += AdminCommandsMessage.FormatWith(
                ListOf(chatFacade.Commands.Where(command => command.RequiresAdmin)));
        }

        chatSimulation.SendMessageAsServerToPlayer(message, sender);
    }

    private static string ListOf(IEnumerable<IChatCommand> commands) =>
        string.Join("\n", commands.Select(command => CommandFormat.FormatWith(command.Name, command.Description)));
}
