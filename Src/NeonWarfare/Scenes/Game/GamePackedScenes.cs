using Godot;
using GodotBox;
using KludgeBox.DI.Requests.NotNullCheck;
using GodotBox.Godot.Nodes;

namespace NeonWarfare.Scenes.Game;

public partial class GamePackedScenes : CheckedAbstractStorage
{
    
    [Export] [NotNull] public PackedScene Hud { get; private set; }
    [Export] [NotNull] public PackedScene ServerHud { get; private set; }
}
