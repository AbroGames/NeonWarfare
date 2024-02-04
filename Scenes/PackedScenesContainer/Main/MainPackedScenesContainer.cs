using Godot;

public partial class MainPackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    [Export] [NotNull] public PackedScene BattleWorld { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}