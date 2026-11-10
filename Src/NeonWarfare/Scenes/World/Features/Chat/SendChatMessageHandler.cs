using System.Globalization;
using System.Linq;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;

namespace NeonWarfare.Scenes.World.Features.Chat;

[CommandHandler]
public class SendChatMessageHandler(ChatSimulationFacade chatSimulationFacade)
    : IPlayerCommandHandler<SendChatMessageCommand>
{
    private const int MessageMaxLength = 1024;

    public bool Validate(string senderUid, SendChatMessageCommand command)
    {
        string text = command.Text;
        if (string.IsNullOrWhiteSpace(text) || text.Length > MessageMaxLength)
        {
            return false;
        }

        return !text.Any(IsForbiddenChar);
    }

    public void Process(string senderUid, SendChatMessageCommand command) =>
        chatSimulationFacade.HandleInput(senderUid, command.Text);

    private static bool IsForbiddenChar(char c) =>
        char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
}
