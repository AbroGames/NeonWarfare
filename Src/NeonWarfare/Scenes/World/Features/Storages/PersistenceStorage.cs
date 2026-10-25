using Godot;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Features.Storages;

/// <summary>
/// The entity holding <see cref="PersistenceModel"/>: one per world, found through
/// <see cref="PersistenceStorageQuery"/>.
/// </summary>
public partial class PersistenceStorage : Node
{
    [Replicated] public readonly PersistenceModel Model = new();
}
