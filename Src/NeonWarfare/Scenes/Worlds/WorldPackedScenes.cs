using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using GodotBox.Godot.Nodes;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// The scenes the World spawns.
/// It belongs to <c>Game</c>, not to the World, because a client needs the protocol hash before its World exists.
/// </summary>
public partial class WorldPackedScenes : CheckedAbstractStorage
{
    [ExportGroup("Surfaces")]
    [Export] [NotNull] public PackedScene SafeSurface { get; private set; }
    [Export] [NotNull] public PackedScene BattleSurface { get; private set; }
}
