using System.Globalization;
using System.Linq;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;

namespace NeonWarfare.Scenes.World.Features.Chat;

[CommandHandler]
public class SendChatMessageHandler(ChatSimulationFacade chatSimulationFacade)
    : IPlayerCommandHandler<SendChatMessageCommand>
{
    private const int MessageMaxLength = 1024;

    public bool Validate(PlayerModel sender, SendChatMessageCommand command)
    {
        string text = command.Text;
        if (string.IsNullOrWhiteSpace(text) || text.Length > MessageMaxLength)
        {
            return false;
        }

        return !text.Any(IsForbiddenChar);
    }

    public void Process(PlayerModel sender, SendChatMessageCommand command) =>
        chatSimulationFacade.HandleInput(sender, command.Text);

    private static bool IsForbiddenChar(char c) =>
        char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
}
