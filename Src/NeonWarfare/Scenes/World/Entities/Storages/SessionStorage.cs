using Godot;
using NeonWarfare.Scenes.World.Models;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Entities.Storages;

/// <summary>
/// The entity holding <see cref="SessionModel"/>: one per world, found through
/// <see cref="Queries.SessionStorageQuery"/>.<br/><br/>
/// Not saved: the session is who is online now etc., and after a load nobody is until the players join again. The
/// save keeps the node itself (scene, NetId, parent), so the load spawns it from the scene with the empty model of the
/// field initializer, and the joins fill it in.
/// </summary>
[NotSaved]
public partial class SessionStorage : Node
{
    [Replicated] public readonly SessionModel Model = new();
}
