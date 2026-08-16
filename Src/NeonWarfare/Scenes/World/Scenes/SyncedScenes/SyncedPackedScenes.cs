using Godot;
using GodotBox;
using KludgeBox.DI.Requests.NotNullCheck;
using GameCheckedAbstractStorage = NeonWarfare.Scenes.Misc.GameCheckedAbstractStorage;

namespace NeonWarfare.Scenes.World.Scenes.SyncedScenes;

public partial class SyncedPackedScenes : GameCheckedAbstractStorage
{
    
    [ExportGroup("Surfaces")]
    [Export] [NotNull] public PackedScene SafeSurface { get; private set; }
    [Export] [NotNull] public PackedScene BattleSurface { get; private set; }
    
    [ExportGroup("Entities")]
    [Export] [NotNull] public PackedScene Character { get; private set; }
    [Export] [NotNull] public PackedScene Wall { get; private set; }
}