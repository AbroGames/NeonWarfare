using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The entity holding <see cref="PlayersSessionModel"/>: one per world, found through
/// <see cref="PlayersSessionStorageQuery"/>.<br/><br/>
/// Not saved: the session is who is online now etc., and after a load nobody is until the players join again. The
/// save keeps the node itself (kind, NetId, parent), so the load creates it from its kind with the empty model of the
/// field initializer, and the joins fill it in.
/// </summary>
[NotSaved]
public partial class PlayersSessionStorage : Node
{
    [Replicated] public readonly PlayersSessionModel Model = new();
}
