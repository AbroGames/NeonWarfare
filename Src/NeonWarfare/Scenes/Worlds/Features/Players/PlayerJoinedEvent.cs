using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The fact of a join, apart from its chat line. A client learns from its own one that its player is online.
/// </summary>
[MessagePackObject]
public record PlayerJoinedEvent([property: Key(0)] PlayerUid Uid) : Event;
