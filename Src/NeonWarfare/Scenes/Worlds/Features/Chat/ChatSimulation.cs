using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scripts;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[Simulation]
public class ChatSimulation(TimeProvider timeProvider, EventOutbox outbox, PlayerQuery players)
{
    private readonly ILogger _log = LogFactory.GetForStatic<ChatSimulation>();

    public void SendMessageAsPlayerToAll(PlayerUid senderUid, string text)
    {
        PlayerModel sender = players.Get(senderUid);
        _log.Information("Chat {nick} ({uid}) to all: {text}", sender.Nick, sender.Uid, ToOneLine(text));
        var message = new ChatPlayerMessageEvent(NowUnixSeconds(), sender.Uid, sender.Nick, text);
        outbox.PublishToAll(message);
    }

    public void SendMessageAsServerToPlayer(string text, PlayerUid receiverUid)
    {
        PlayerModel receiver = players.Get(receiverUid);
        _log.Information("Chat server to {nick} ({uid}): {text}", receiver.Nick, receiver.Uid, ToOneLine(text));
        var message = new ChatServerMessageEvent(NowUnixSeconds(), text);
        outbox.PublishTo(message, receiverUid);
    }

    public void SendLocalizedMessageAsServerToAll(string key, string[] args)
    {
        _log.Information("Chat server to all: {key} {args}", key, args);
        outbox.PublishToAll(new LocalizedChatMessageEvent(NowUnixSeconds(), new LocalizedText(key, args)));
    }

    public void SendLocalizedMessageAsServerToPlayer(string key, string[] args, PlayerUid receiverUid)
    {
        PlayerModel receiver = players.Get(receiverUid);
        _log.Information("Chat server to {nick} ({uid}): {key} {args}", receiver.Nick, receiver.Uid, key, args);
        outbox.PublishTo(new LocalizedChatMessageEvent(NowUnixSeconds(), new LocalizedText(key, args)), receiverUid);
    }

    // A log record stays on one line, because a continuation line has no time or source and grep loses it.
    private string ToOneLine(string text) => text.ReplaceLineEndings("\\n");

    private long NowUnixSeconds() => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
