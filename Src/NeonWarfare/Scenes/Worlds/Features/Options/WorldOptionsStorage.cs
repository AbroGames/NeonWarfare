using Godot;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Options;

/// <summary>
/// The entity holding <see cref="WorldOptions"/>, one per world, found through <see cref="WorldOptionsQuery"/>.
/// </summary>
public partial class WorldOptionsStorage : Node
{
    // Not readonly, because the options of a new world are set when the storage is spawned
    [Replicated] public WorldOptions Options = WorldOptions.GetDefault();
}
