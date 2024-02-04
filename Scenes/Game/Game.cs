using Godot;

namespace AbroDraft;

public partial class Game : Node2D
{
	
	[Export] [NotNull] public NodeContainer MainSceneContainer { get; private set; }
	[Export] [NotNull] public PlayerInfo PlayerInfo { get; private set; }
	
	public override void _Ready()
	{
        NotNullChecker.CheckProperties(this);
        
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
	}
}