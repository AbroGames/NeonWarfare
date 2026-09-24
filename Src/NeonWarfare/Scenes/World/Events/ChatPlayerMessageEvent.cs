using MessagePack;

namespace NeonWarfare.Scenes.World.Events;

[MessagePackObject]
public record ChatPlayerMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string SenderUid,
    [property: Key(2)] string SenderNick,
    [property: Key(3)] string Text);