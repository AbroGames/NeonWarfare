using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Events;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.ServerNetwork;
using Serilog;

namespace NeonWarfare.Scenes.World.Simulations;

// The log line is written here, not by a reader of the events: a headless dedicated server has no Presentation,
// and this is the one place every chat message passes
[Simulation]
public class ChatSimulation(TimeProvider timeProvider, EventOutbox outbox)
{
    private const string PlayerToAllLog = "Chat {nick} ({uid}) to all: {text}";
    private const string ServerToAllLog = "Chat server to all: {text}";
    private const string ServerToPlayerLog = "Chat server to {nick} ({uid}): {text}";
    private const string ServerToDedicatedWindowLog = "Chat server to the dedicated window: {text}";

    private readonly ILogger _log = LogFactory.GetForStatic<ChatSimulation>();

    public void SendMessageAsPlayerToAll(PlayerModel sender, string text)
    {
        _log.Information(PlayerToAllLog, sender.Nick, sender.Uid, text);
        outbox.PublishToAll(CreateChatPlayerMessageEvent(sender, text));
    }

    public void SendMessageAsServerToAll(string text)
    {
        _log.Information(ServerToAllLog, text);
        outbox.PublishToAll(CreateChatServerMessageEvent(text));
    }

    public void SendMessageAsServerToPlayer(string text, PlayerModel receiver)
    {
        _log.Information(ServerToPlayerLog, receiver.Nick, receiver.Uid, text);
        outbox.PublishTo(CreateChatServerMessageEvent(text), receiver);
    }

    public void SendMessageAsServerToDedicatedWindow(string text)
    {
        _log.Information(ServerToDedicatedWindowLog, text);
        outbox.PublishToDedicatedWindow(CreateChatServerMessageEvent(text));
    }
    
    private ChatPlayerMessageEvent CreateChatPlayerMessageEvent(PlayerModel sender, string text)
    {
        return new ChatPlayerMessageEvent(NowUnixSeconds(), sender.Uid, sender.Nick, text);
    }

    private ChatServerMessageEvent CreateChatServerMessageEvent(string text)
    {
        return new ChatServerMessageEvent(NowUnixSeconds(), text);
    }

    private long NowUnixSeconds() => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
