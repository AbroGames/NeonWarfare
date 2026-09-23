using MessagePack;

namespace NeonWarfare.Scenes.World.Events;

[MessagePackObject]
public record ChatServerMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Text);