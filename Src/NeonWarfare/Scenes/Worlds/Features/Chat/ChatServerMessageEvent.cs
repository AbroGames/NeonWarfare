using MessagePack;
using NeonWarfare.Scenes.World.Infra.Protocol;

namespace NeonWarfare.Scenes.World.Features.Chat;

[MessagePackObject]
public record ChatServerMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Text) : Event;
