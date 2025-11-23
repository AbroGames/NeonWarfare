using Godot;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Game;

public partial class GamePackedScenes : Node
{
    
    [Export] [NotNull] public PackedScene World { get; private set; }
    [Export] [NotNull] public PackedScene Hud { get; private set; }

    public override void _Ready()
    {
        Di.Process(this); 
    }
}