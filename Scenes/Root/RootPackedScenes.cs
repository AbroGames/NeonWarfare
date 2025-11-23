using Godot;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Root;

public partial class RootPackedScenes : Node
{
    
    [Export] [NotNull] public PackedScene Game { get; private set; }
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    [Export] [NotNull] public PackedScene LoadingScreen { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
    }
}