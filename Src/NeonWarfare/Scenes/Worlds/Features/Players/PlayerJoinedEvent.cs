using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The fact of the join, apart from its chat line: a client learns from its own one that its player is online.
/// </summary>
[MessagePackObject]
public record PlayerJoinedEvent([property: Key(0)] string Uid) : Event;
