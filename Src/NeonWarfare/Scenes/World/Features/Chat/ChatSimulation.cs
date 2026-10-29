using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Events;
using Serilog;

namespace NeonWarfare.Scenes.World.Features.Chat;

[Simulation]
public class ChatSimulation(TimeProvider timeProvider, EventOutbox outbox, PlayerQuery players)
{
    private const string PlayerToAllLog = "Chat {nick} ({uid}) to all: {text}";
    private const string ServerToPlayerLog = "Chat server to {nick} ({uid}): {text}";

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

    // A log record is one line: a continuation line has no time or source, and grep loses it
    private string ToOneLine(string text) => text.ReplaceLineEndings("\\n");

    private long NowUnixSeconds() => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
