using Godot;

public partial class References : Node
{
    [Export] public WorldContainer WorldContainer { get; private set; }
    [Export] public HudContainer HudContainer { get; private set; }
    [Export] public MenuContainer MenuContainer { get; private set; }
    
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