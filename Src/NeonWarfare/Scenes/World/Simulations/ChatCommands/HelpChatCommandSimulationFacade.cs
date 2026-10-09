using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using NeonWarfare.Scenes.World.Composition;

namespace NeonWarfare.Scenes.World.Simulations.ChatCommands;

[Simulation(Facade = true)]
public class HelpChatCommandSimulationFacade(ChatSimulationFacade chat) : IChatCommand
{
    private const string PlayerCommandsMessage = "\nPlayer commands:\n{0}";
    private const string AdminCommandsMessage = "\nAdmin commands:\n{0}";
    private const string CommandFormat = "   '/{0}' -> {1}";

    public string Name => "help";
    public string Description => "Show list of available commands.";
    public bool RequiresAdmin => false;

    public void Execute(bool isAdmin, Action<string> reply, string arguments)
    {
        string message = PlayerCommandsMessage.FormatWith(
            ListOf(chat.Commands.Where(command => !command.RequiresAdmin)));
        if (isAdmin)
        {
            message += AdminCommandsMessage.FormatWith(
                ListOf(chat.Commands.Where(command => command.RequiresAdmin)));
        }

        reply(message);
    }

    private static string ListOf(IEnumerable<IChatCommand> commands) =>
        string.Join("\n", commands.Select(command => CommandFormat.FormatWith(command.Name, command.Description)));
}
