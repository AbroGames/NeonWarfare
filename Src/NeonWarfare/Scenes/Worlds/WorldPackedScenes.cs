using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using GodotBox.Godot.Nodes;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// The scenes World spawns. <see cref="GodotBox.Godot.Nodes.AbstractStorage.GetScenesList"/>, i.e. the declaration
/// order of the properties, is the order of the scene kinds of the <see cref="EntityCatalog"/>, so it is part of the
/// protocol. Owned by Game, not World: the client needs the hash before its World exists.
/// </summary>
public partial class WorldPackedScenes : CheckedAbstractStorage
{
    [ExportGroup("Surfaces")]
    [Export] [NotNull] public PackedScene SafeSurface { get; private set; }
    [Export] [NotNull] public PackedScene BattleSurface { get; private set; }
}
