using System;
using NeonWarfare.Scenes.OldWorld.WorldServices.Chat;

namespace NeonWarfare.Scenes.OldWorld.WorldServices.Command;

public class ChatMessageCommandInterceptor : IChatMessageInterceptor
{
    private readonly Action<int, string> _onCommand;

    public ChatMessageCommandInterceptor(Action<int, string> onCommand)
    {
        _onCommand = onCommand;
    }

    public bool IsPass(int senderId, string text)
    {
        if (text.StartsWith('/'))
        {
            _onCommand.Invoke(senderId, text.Substring(1));
            return false;
        }
        return true;
    }
}