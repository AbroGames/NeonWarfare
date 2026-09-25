using System;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Simulations;

public class ChatSimulation(TimeProvider timeProvider)
{
    // TODO
    // public void SendMessageAsPlayerToAll(PlayerModel sender, string text) =>
    //     outbox.PublishToAll(CreateChatPlayerMessageEvent(sender.Uid, sender.Nick, text));
    //
    // public void SendMessageAsServerToAll(string text) =>
    //     outbox.PublishToAll(CreateChatServerMessageEvent(text));
    //
    // public void SendMessageAsServerToPlayer(string text, PlayerModel receiver) =>
    //     outbox.PublishTo(CreateChatServerMessageEvent(text), receiver);

    private ChatPlayerMessageEvent CreateChatPlayerMessageEvent(string senderUid, string senderNick, string text)
    {
        return new ChatPlayerMessageEvent(NowUnixSeconds(), senderUid, senderNick, text);
    }

    private ChatServerMessageEvent CreateChatServerMessageEvent(string text)
    {
        return new ChatServerMessageEvent(NowUnixSeconds(), text);
    }

    private long NowUnixSeconds() => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
