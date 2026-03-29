using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.Root;

public partial class RootPackedScenes : CheckedAbstractStorage
{
    
    [Export] [NotNull] public PackedScene Game { get; private set; }
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    [Export] [NotNull] public PackedScene LoadingScreen { get; private set; }
}