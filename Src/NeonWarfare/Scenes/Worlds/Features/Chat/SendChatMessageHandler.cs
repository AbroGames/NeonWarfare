using KludgeBox.Core.Text;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[CommandHandler]
public class SendChatMessageHandler(ChatSimulationFacade chatSimulationFacade)
    : IPlayerCommandHandler<SendChatMessageCommand>
{
    private const int MessageMaxLength = 1024;

    public bool Validate(PlayerUid senderUid, SendChatMessageCommand command)
    {
        string text = command.Text;
        return !string.IsNullOrWhiteSpace(text) && text.Length <= MessageMaxLength && text.IsSingleLinePrintable();
    }

    public void Process(PlayerUid senderUid, SendChatMessageCommand command) =>
        chatSimulationFacade.HandleChatInput(senderUid, command.Text);
}
