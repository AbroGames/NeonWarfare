using Godot;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The entity holding <see cref="PlayersModel"/>: one per world, found through
/// <see cref="PlayersStorageQuery"/>.
/// </summary>
public partial class PlayersStorage : Node
{
    [Replicated] public readonly PlayersModel Model = new();
}
