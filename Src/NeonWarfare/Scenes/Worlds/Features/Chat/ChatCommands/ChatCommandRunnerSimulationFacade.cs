using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;

[SimulationFacade]
public class ChatCommandRunnerSimulationFacade
{
    private const string NotFoundReply = "Command '{0}' not found. Use '/help' to see the list of commands.";
    private const string RequiresAdminReply = "Command '{0}' requires admin status.";

    private readonly ILogger _log = LogFactory.GetForStatic<ChatCommandRunnerSimulationFacade>();

    private readonly ChatSimulation _chatSimulation;
    private readonly PlayerQuery _players;
    private readonly Dictionary<string, IChatCommand> _commandByName;

    public ChatCommandRunnerSimulationFacade(
        ChatSimulation chatSimulation, PlayerQuery players, IEnumerable<IChatCommand> commands)
    {
        _chatSimulation = chatSimulation;
        _players = players;

        var commandByName = new Dictionary<string, IChatCommand>(StringComparer.Ordinal);
        foreach (IChatCommand command in commands)
        {
            string name = command.Name;
            if (string.IsNullOrEmpty(name) || name.Any(char.IsWhiteSpace) || name != name.ToLowerInvariant())
            {
                throw new InvalidOperationException($"{command.GetType().FullName} has the name '{name}': a chat "
                                                    + "command name is lower case, with no whitespace.");
            }
            if (commandByName.TryGetValue(name, out IChatCommand existing))
            {
                throw new InvalidOperationException(
                    $"{existing.GetType().Name} and {command.GetType().Name} both have the name '{name}'.");
            }
            commandByName.Add(name, command);
        }

        _commandByName = commandByName;
    }

    /// <summary>The text comes without the leading <c>/</c>.</summary>
    public void Run(PlayerUid senderUid, string text)
    {
        PlayerModel sender = _players.Get(senderUid);

        // ChatSimulation logs only chat messages, so a command is logged here.
        _log.Information("{nick} ({uid}) ran command /{text}", sender.Nick, sender.Uid, text);

        int nameEnd = 0;
        while (nameEnd < text.Length && !char.IsWhiteSpace(text[nameEnd]))
        {
            nameEnd++;
        }
        string name = text[..nameEnd].ToLowerInvariant();
        string arguments = text[nameEnd..].Trim();

        if (!_commandByName.TryGetValue(name, out IChatCommand command))
        {
            _chatSimulation.SendMessageAsServerToPlayer(NotFoundReply.FormatWith(name), senderUid);
            return;
        }
        if (command.RequiresAdmin && !sender.IsAdmin)
        {
            _chatSimulation.SendMessageAsServerToPlayer(RequiresAdminReply.FormatWith(name), senderUid);
            return;
        }

        command.Execute(senderUid, arguments);
    }
}
