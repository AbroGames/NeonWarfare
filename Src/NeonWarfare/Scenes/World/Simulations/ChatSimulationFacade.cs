using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Simulations.ChatCommands;
using Serilog;

namespace NeonWarfare.Scenes.World.Simulations;

[SimulationFacade]
public class ChatSimulationFacade(ChatSimulation chatSimulation)
{
    private const string RanLog = "{nick} ({uid}) ran /{text}";
    private const string NotFoundReply = "Command '{0}' not found. Use '/help' to see the list of commands.";
    private const string RequiresAdminReply = "Command '{0}' requires admin status.";

    private const string RegisteredError = "The chat commands are already registered.";
    private const string NotRegisteredError = "The chat commands are not registered yet.";
    private const string BadNameError =
        "{0} has the name '{1}': a chat command name is lower case, with no whitespace.";
    private const string SecondCommandError = "{0} and {1} both have the name '{2}'.";

    private readonly ILogger _log = LogFactory.GetForStatic<ChatSimulationFacade>();

    private Dictionary<string, IChatCommand> _commandByName;
    public IReadOnlyCollection<IChatCommand> Commands =>
        _commandByName.Values.OrderBy(command => command.Name, StringComparer.Ordinal).ToList();

    // From the composition root rather than the constructor: /help takes this facade, so the list would close a cycle
    public void Register(IEnumerable<IChatCommand> commands)
    {
        if (_commandByName != null)
        {
            throw new InvalidOperationException(RegisteredError);
        }

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

    public void HandleInput(PlayerModel sender, string text)
    {
        if (text.StartsWith('/'))
        {
            ExecuteChatCommand(sender, text[1..]);
            return;
        }
        chatSimulation.SendMessageAsPlayerToAll(sender, text);
    }

    private void ExecuteChatCommand(PlayerModel sender, string text)
    {
        if (_commandByName == null)
        {
            throw new InvalidOperationException(NotRegisteredError);
        }

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
            chatSimulation.SendMessageAsServerToPlayer(NotFoundReply.FormatWith(name), sender);
            return;
        }
        if (command.RequiresAdmin && !sender.IsAdmin)
        {
            chatSimulation.SendMessageAsServerToPlayer(RequiresAdminReply.FormatWith(name), sender);
            return;
        }

        command.Execute(sender, arguments);
    }
}
