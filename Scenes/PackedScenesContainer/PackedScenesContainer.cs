using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.PackedScenesContainer;

public partial class PackedScenesContainer : Node
{
    [Export] [NotNull] public Main.MainPackedScenesContainer Main { get; private set; }
    [Export] [NotNull] public World.WorldPackedScenesContainer World { get; private set; }
    [Export] [NotNull] public Screen.ScreenPackedScenesContainer Screen { get; private set; }
    [Export] [NotNull] public Effects.EffectsPackedScenesContainer Effects { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}