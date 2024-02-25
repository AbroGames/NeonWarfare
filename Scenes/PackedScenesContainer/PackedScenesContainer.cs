using Godot;
using KludgeBox;

public partial class PackedScenesContainer : Node
{
    [Export] [NotNull] public MainPackedScenesContainer Main { get; private set; }
    [Export] [NotNull] public WorldPackedScenesContainer World { get; private set; }
    [Export] [NotNull] public ScreenPackedScenesContainer Screen { get; private set; }
    [Export] [NotNull] public EffectsPackedScenesContainer Effects { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}