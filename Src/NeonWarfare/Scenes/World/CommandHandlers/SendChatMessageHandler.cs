using NeonWarfare.Scenes.World.Commands;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.CommandHandlers;

public class SendChatMessageHandler : IPlayerCommandHandler<SendChatMessageCommand>
{
    public bool Validate(PlayerModel sender, SendChatMessageCommand command)
    {
        //TODO Проверка на null, Валидация длины, допустимых символов, что-то ещё?
        //TODO Вокруг Validate в общем сервисе обработки поставить ловлю любых ошибок и расценивать как false
        return true;
    }

    public void Process(PlayerModel sender, SendChatMessageCommand command)
    {
        if (command.Text.StartsWith('/'))
        {
            //ChatCommandSimulation.ProcessChatCommand(sender, command.Text.Substring(1))
            return;
        }
        //ChatSimulation.SendMessage(sender, command)
    }
}