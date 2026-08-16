using Godot;
using GodotBox;
using KludgeBox.DI.Requests.NotNullCheck;
using GameCheckedAbstractStorage = NeonWarfare.Scenes.Misc.GameCheckedAbstractStorage;

namespace NeonWarfare.Scenes.Root;

public partial class RootPackedScenes : GameCheckedAbstractStorage
{
    
    [Export] [NotNull] public PackedScene Game { get; private set; }
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    [Export] [NotNull] public PackedScene LoadingScreen { get; private set; }
}