using MessagePack;
using NeonWarfare.Scenes.World.Infra.Protocol;

namespace NeonWarfare.Scenes.World.Features.Players;

/// <summary>
/// The fact of the join, apart from its chat line: a client learns from its own one that its player is online.
/// </summary>
[MessagePackObject]
public record PlayerJoinedEvent([property: Key(0)] string Uid) : Event;
