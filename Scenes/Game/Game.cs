using Godot;

namespace AbroDraft;

public partial class Game : Node2D
{
	
	[Export] [NotNull] public NodeContainer WorldContainer { get; private set; }
	[Export] [NotNull] public NodeContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public NodeContainer HudContainer { get; private set; }
	[Export] [NotNull] public NodeContainer MenuContainer { get; private set; }
	[Export] [NotNull] public NodeContainer ForegroundContainer { get; private set; }
	
	[Export] [NotNull] public PlayerInfo PlayerInfo { get; private set; }
	
	public override void _Ready()
	{
        NotNullChecker.CheckProperties(this);
        
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		var firstScene = Root.Instance.PackedScenes.Screen.FirstScene;
		MenuContainer.ChangeStoredNode(firstScene.Instantiate() as Control);
	}
}