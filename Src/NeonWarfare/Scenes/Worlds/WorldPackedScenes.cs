using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using GodotBox.Godot.Nodes;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// The scenes the World spawns.
/// The declaration order of the properties is the order of the scene kinds in the <see cref="EntityCatalog"/>, so it
/// is part of the protocol.
/// It belongs to <c>Game</c>, not to the World, because a client needs the protocol hash before its World exists.
/// </summary>
public partial class WorldPackedScenes : CheckedAbstractStorage
{
    [ExportGroup("Surfaces")]
    [Export] [NotNull] public PackedScene SafeSurface { get; private set; }
    [Export] [NotNull] public PackedScene BattleSurface { get; private set; }
}
