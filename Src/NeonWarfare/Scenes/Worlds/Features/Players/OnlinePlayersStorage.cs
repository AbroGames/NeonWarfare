using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The entity holding <see cref="OnlinePlayersModel"/>: one per world, found through
/// <see cref="OnlinePlayersStorageQuery"/>.<br/><br/>
/// Not saved: after a load nobody is online until the players join again. The save keeps the node itself (kind,
/// NetId, parent), so the load creates it from its kind with the empty model of the field initializer, and the joins
/// fill it in.
/// </summary>
[NotSaved]
public partial class OnlinePlayersStorage : Node
{
    [Replicated] public readonly OnlinePlayersModel Model = new();
}
