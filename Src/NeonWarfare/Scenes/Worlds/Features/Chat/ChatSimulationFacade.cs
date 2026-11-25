using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[SimulationFacade]
public class ChatSimulationFacade
{
    private const string RanLog = "{nick} ({uid}) ran command /{text}";
    private const string NotFoundReply = "Command '{0}' not found. Use '/help' to see the list of commands.";
    private const string RequiresAdminReply = "Command '{0}' requires admin status.";

    private const string BadNameError =
        "{0} has the name '{1}': a chat command name is lower case, with no whitespace.";
    private const string SecondCommandError = "{0} and {1} both have the name '{2}'.";

    private readonly ILogger _log = LogFactory.GetForStatic<ChatSimulationFacade>();

    private readonly ChatSimulation _chatSimulation;
    private readonly PlayerQuery _players;
    private readonly Dictionary<string, IChatCommand> _commandByName;

    public ChatSimulationFacade(ChatSimulation chatSimulation, PlayerQuery players, IEnumerable<IChatCommand> commands)
    {
        _chatSimulation = chatSimulation;
        _players = players;

        var commandByName = new Dictionary<string, IChatCommand>(StringComparer.Ordinal);
        foreach (IChatCommand command in commands)
        {
            string name = command.Name;
            if (string.IsNullOrEmpty(name) || name.Any(char.IsWhiteSpace) || name != name.ToLowerInvariant())
            {
                throw new InvalidOperationException(BadNameError.FormatWith(command.GetType().FullName, name));
            }
            if (commandByName.TryGetValue(name, out IChatCommand existing))
            {
                throw new InvalidOperationException(
                    SecondCommandError.FormatWith(existing.GetType().Name, command.GetType().Name, name));
            }
            commandByName.Add(name, command);
        }

        _commandByName = commandByName;
    }

    public void HandleInput(string senderUid, string text)
    {
        if (text.StartsWith('/'))
        {
            ExecuteChatCommand(senderUid, text[1..]);
            return;
        }
        _chatSimulation.SendMessageAsPlayerToAll(senderUid, text);
    }

    private void ExecuteChatCommand(string senderUid, string text)
    {
        PlayerModel sender = _players.Get(senderUid);

        // A command is not a chat message, so ChatSimulation never logs it, we have to log it here
        _log.Information(RanLog, sender.Nick, sender.Uid, text);

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
