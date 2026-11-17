using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[MessagePackObject]
public record ChatServerMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Text) : Event;
