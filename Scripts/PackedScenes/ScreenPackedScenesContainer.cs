using Godot;

public partial class ScreenPackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    
    public PackedScene FirstScene { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        
        FirstScene = MainMenu;
    }
}