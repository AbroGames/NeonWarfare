using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.Game;

public partial class GamePackedScenes : CheckedAbstractStorage
{
    
    [Export] [NotNull] public PackedScene World { get; private set; }
    [Export] [NotNull] public PackedScene Hud { get; private set; }
    [Export] [NotNull] public PackedScene ServerHud { get; private set; }
}