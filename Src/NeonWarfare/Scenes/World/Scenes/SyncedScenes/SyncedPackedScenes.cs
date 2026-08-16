using Godot;
using GodotBox;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.World.Scenes.SyncedScenes;

public partial class SyncedPackedScenes : CheckedAbstractStorage
{
    
    [ExportGroup("Surfaces")]
    [Export] [NotNull] public PackedScene SafeSurface { get; private set; }
    [Export] [NotNull] public PackedScene BattleSurface { get; private set; }
    
    [ExportGroup("Entities")]
    [Export] [NotNull] public PackedScene Character { get; private set; }
    [Export] [NotNull] public PackedScene Wall { get; private set; }
}