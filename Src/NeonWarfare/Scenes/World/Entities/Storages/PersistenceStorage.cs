using Godot;
using NeonWarfare.Scenes.World.Models;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Entities.Storages;

/// <summary>
/// The entity holding <see cref="PersistenceModel"/>: one per world, found through
/// <see cref="Queries.PersistenceStorageQuery"/>.
/// </summary>
public partial class PersistenceStorage : Node
{
    [Replicated] public readonly PersistenceModel Model = new();
}
