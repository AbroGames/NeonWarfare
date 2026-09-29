using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Simulations;

namespace NeonWarfare.Scenes.World.CommandHandlers;

[CommandHandler]
public class SendChatMessageHandler(ChatSimulationFacade chatSimulationFacade)
    : IPlayerCommandHandler<SendChatMessageCommand>
{
    public bool Validate(PlayerModel sender, SendChatMessageCommand command)
    {
        //TODO Проверка на null, Валидация длины, допустимых символов, что-то ещё?
        //TODO Вокруг Validate в общем сервисе обработки поставить ловлю любых ошибок и расценивать как false
        return true;
    }

    public void Process(PlayerModel sender, SendChatMessageCommand command) =>
        chatSimulationFacade.SendMessage(sender, command.Text);
}
