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

/// <summary>
/// A line typed into the chat: a chat command runs with the rights and the reply address of its sender, any other
/// text is a chat message. Messages and replies are built and logged by <see cref="ChatSimulation"/>, which other
/// facades call directly for their own server messages.
/// </summary>
[Simulation(Facade = true)]
public class ChatSimulationFacade(ChatSimulation chatSimulation)
{
    private const string RanLog = "{sender} ran /{text}";
    private const string PlayerSender = "{0} ({1})";
    private const string DedicatedWindowSender = "the dedicated window";
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

    public void HandleInputFromPlayer(PlayerModel sender, string text)
    {
        if (IsChatCommand(text))
        {
            ExecuteChatCommand(
                PlayerSender.FormatWith(sender.Nick, sender.Uid),
                sender.IsAdmin,
                reply => chatSimulation.SendMessageAsServerToPlayer(reply, sender),
                GetChatCommand(text));
            return;
        }
        chatSimulation.SendMessageAsPlayerToAll(sender, text);
    }

    public void HandleInputFromDedicatedWindow(string text)
    {
        if (IsChatCommand(text))
        {
            ExecuteChatCommand(
                DedicatedWindowSender,
                true,
                chatSimulation.SendMessageAsServerToDedicatedWindow,
                GetChatCommand(text));
            return;
        }
        chatSimulation.SendMessageAsServerToAll(text);
    }

    private bool IsChatCommand(string text) => text.StartsWith('/');
    private string GetChatCommand(string text) => text[1..];

    private void ExecuteChatCommand(string sender, bool isAdmin, Action<string> reply, string text)
    {
        if (_commandByName == null)
        {
            throw new InvalidOperationException(NotRegisteredError);
        }

        // A command is not a chat message, so ChatSimulation never logs it
        _log.Information(RanLog, sender, text);

        int nameEnd = 0;
        while (nameEnd < text.Length && !char.IsWhiteSpace(text[nameEnd]))
        {
            nameEnd++;
        }
        string name = text[..nameEnd].ToLowerInvariant();
        string arguments = text[nameEnd..].Trim();

        if (!_commandByName.TryGetValue(name, out IChatCommand command))
        {
            reply(NotFoundReply.FormatWith(name));
            return;
        }
        if (command.RequiresAdmin && !isAdmin)
        {
            reply(RequiresAdminReply.FormatWith(name));
            return;
        }

        command.Execute(isAdmin, reply, arguments);
    }
}
