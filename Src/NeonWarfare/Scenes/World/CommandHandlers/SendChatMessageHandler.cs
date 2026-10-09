using System.Globalization;
using System.Linq;
using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Simulations;

namespace NeonWarfare.Scenes.World.CommandHandlers;

// One handler for both senders: the dedicated window's text is typed by a human and reaches every player too, so it
// follows the same rule
[CommandHandler]
public class SendChatMessageHandler(ChatSimulationFacade chatSimulationFacade)
    : IPlayerCommandHandler<SendChatMessageCommand>, IDedicatedWindowCommandHandler<SendChatMessageCommand>
{
    private const int MessageMaxLength = 1024;

    public bool Validate(PlayerModel sender, SendChatMessageCommand command) => 
        IsValid(command.Text);

    public void Process(PlayerModel sender, SendChatMessageCommand command) =>
        chatSimulationFacade.HandleInputFromPlayer(sender, command.Text);

    public bool Validate(SendChatMessageCommand command) => 
        IsValid(command.Text);

    public void Process(SendChatMessageCommand command) =>
        chatSimulationFacade.HandleInputFromDedicatedWindow(command.Text);

    private static bool IsValid(string text) =>
        !string.IsNullOrWhiteSpace(text)
        && text.Length <= MessageMaxLength
        && text.All(c => char.GetUnicodeCategory(c) is not (UnicodeCategory.Control or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator));
}
