using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

/// <summary>
/// A server message the receiver translates itself, see Docs/Chat-and-commands.md#localized-server-messages.
/// </summary>
[MessagePackObject]
public record LocalizedChatMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] LocalizedText Text) : Event;
