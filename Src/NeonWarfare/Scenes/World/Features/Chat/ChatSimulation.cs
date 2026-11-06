using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;
using Serilog;

namespace NeonWarfare.Scenes.World.Features.Chat;

[Simulation]
public class ChatSimulation(TimeProvider timeProvider, EventOutbox outbox)
{
    private const string PlayerToAllLog = "Chat {nick} ({uid}) to all: {text}";
    private const string ServerToPlayerLog = "Chat server to {nick} ({uid}): {text}";

    private readonly ILogger _log = LogFactory.GetForStatic<ChatSimulation>();

    public void SendMessageAsPlayerToAll(PlayerModel sender, string text)
    {
        _log.Information(PlayerToAllLog, sender.Nick, sender.Uid, text);
        var message = new ChatPlayerMessageEvent(NowUnixSeconds(), sender.Uid, sender.Nick, text);
        outbox.PublishToAll(message);
    }

    public void SendMessageAsServerToPlayer(string text, PlayerModel receiver)
    {
        _log.Information(ServerToPlayerLog, receiver.Nick, receiver.Uid, text);
        var message = new ChatServerMessageEvent(NowUnixSeconds(), text);
        outbox.PublishTo(message, receiver);
    }

    private long NowUnixSeconds() => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
