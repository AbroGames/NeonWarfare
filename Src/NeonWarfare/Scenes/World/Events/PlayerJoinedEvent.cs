using MessagePack;

namespace NeonWarfare.Scenes.World.Events;

[MessagePackObject]
public record PlayerJoinedEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Uid,
    [property: Key(2)] string Nick);
