using Godot;

public partial class References : Node
{
    [Export] public Node2DContainer WorldContainer { get; private set; }
    [Export] public ControlContainer HudContainer { get; private set; }
    [Export] public ControlContainer MenuContainer { get; private set; }
    
    [Export] public PackedScene FirstSceneBlueprint { get; private set; }
    [Export] public PackedScene CharacterBlueprint { get; private set; }
    [Export] public PackedScene AllyBlueprint { get; private set; }
    [Export] public PackedScene EnemyBlueprint { get; private set; }
    [Export] public PackedScene MainMenu { get; private set; }
    [Export] public Player PlayerInfo { get; private set; }
    
    public static References Instance { get; private set; }
    public override void _Ready()
    {
        Instance = this;
    }
}