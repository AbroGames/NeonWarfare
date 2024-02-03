using Godot;

public partial class PackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene WorldPackedScenesContainerPackedScene { get; private set; }
    [Export] [NotNull] public PackedScene ScreenPackedScenesContainerPackedScene { get; private set; }
    
    public WorldPackedScenesContainer World { get; private set; }
    public ScreenPackedScenesContainer Screen { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        World = WorldPackedScenesContainerPackedScene.Instantiate<WorldPackedScenesContainer>();
        Screen = ScreenPackedScenesContainerPackedScene.Instantiate<ScreenPackedScenesContainer>();
    }
}