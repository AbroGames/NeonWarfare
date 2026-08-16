using Godot;
using GodotBox;
using KludgeBox.DI.Requests.NotNullCheck;
using GameCheckedAbstractStorage = NeonWarfare.Scenes.Misc.GameCheckedAbstractStorage;

namespace NeonWarfare.Scenes.Game;

public partial class GamePackedScenes : GameCheckedAbstractStorage
{
    
    [Export] [NotNull] public PackedScene World { get; private set; }
    [Export] [NotNull] public PackedScene Hud { get; private set; }
    [Export] [NotNull] public PackedScene ServerHud { get; private set; }
}