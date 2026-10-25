using MessagePack;
using NeonWarfare.Scenes.World.Infra.Protocol;

namespace NeonWarfare.Scenes.World.Features.Players;

[MessagePackObject]
public record PlayerLeftEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Uid,
    [property: Key(2)] string Nick) : Event;
