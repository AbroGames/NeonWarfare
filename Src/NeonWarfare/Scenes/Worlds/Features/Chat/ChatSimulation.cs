using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Events;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[Simulation]
public class ChatSimulation(TimeProvider timeProvider, EventOutbox outbox, PlayerQuery players)
{
    private const string PlayerToAllLog = "Chat {nick} ({uid}) to all: {text}";
    private const string ServerToPlayerLog = "Chat server to {nick} ({uid}): {text}";
    private const string LocalizedServerToAllLog = "Chat server to all: {key} {args}";
    private const string LocalizedServerToPlayerLog = "Chat server to {nick} ({uid}): {key} {args}";

    private readonly ILogger _log = LogFactory.GetForStatic<ChatSimulation>();

    public void SendMessageAsPlayerToAll(string senderUid, string text)
    {
        PlayerModel sender = players.Get(senderUid);
        _log.Information(PlayerToAllLog, sender.Nick, sender.Uid, ToOneLine(text));
        var message = new ChatPlayerMessageEvent(NowUnixSeconds(), sender.Uid, sender.Nick, text);
        outbox.PublishToAll(message);
    }

    public void SendMessageAsServerToPlayer(string text, string receiverUid)
    {
        PlayerModel receiver = players.Get(receiverUid);
        _log.Information(ServerToPlayerLog, receiver.Nick, receiver.Uid, ToOneLine(text));
        var message = new ChatServerMessageEvent(NowUnixSeconds(), text);
        outbox.PublishTo(message, receiverUid);
    }

    public void SendLocalizedMessageAsServerToAll(string key, params string[] args)
    {
        _log.Information(LocalizedServerToAllLog, key, args);
        outbox.PublishToAll(new LocalizedChatMessageEvent(NowUnixSeconds(), key, args));
    }

    public void SendLocalizedMessageAsServerToPlayer(string receiverUid, string key, params string[] args)
    {
        PlayerModel receiver = players.Get(receiverUid);
        _log.Information(LocalizedServerToPlayerLog, receiver.Nick, receiver.Uid, key, args);
        outbox.PublishTo(new LocalizedChatMessageEvent(NowUnixSeconds(), key, args), receiverUid);
    }

    // A log record is one line: a continuation line has no time or source, and grep loses it
    private string ToOneLine(string text) => text.ReplaceLineEndings("\\n");

    private long NowUnixSeconds() => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
