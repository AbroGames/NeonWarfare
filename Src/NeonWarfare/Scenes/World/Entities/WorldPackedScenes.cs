using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.Misc;

namespace NeonWarfare.Scenes.World.Entities;

/// <summary>
/// The scenes World spawns. A scene's id is its index in
/// <see cref="GodotBox.Godot.Nodes.AbstractStorage.GetScenesList"/>, i.e. the declaration order of the properties.
/// The id travels in spawn records and lies in saves, so the order is part of the protocol and goes into the
/// protocol hash. Owned by Game, not World: the client needs the hash before its World exists.
/// </summary>
public partial class WorldPackedScenes : GameCheckedAbstractStorage
{
    [ExportGroup("Surfaces")]
    [Export] [NotNull] public PackedScene SafeSurface { get; private set; }
    [Export] [NotNull] public PackedScene BattleSurface { get; private set; }

    [ExportGroup("Storages")]
    [Export] [NotNull] public PackedScene PersistenceStorage { get; private set; }
    [Export] [NotNull] public PackedScene SessionStorage { get; private set; }
}
