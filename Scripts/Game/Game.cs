using Godot;

namespace AbroDraft;

public partial class Game : Node2D
{
	
	[Export] [NotNull] public Node2DContainer WorldContainer { get; private set; }
	[Export] [NotNull] public ControlContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public ControlContainer HudContainer { get; private set; }
	[Export] [NotNull] public ControlContainer MenuContainer { get; private set; }
	[Export] [NotNull] public ControlContainer ForegroundContainer { get; private set; }
	
	[Export] [NotNull] public PlayerInfo PlayerInfo { get; private set; }
	
	public override void _Ready()
	{
        NotNullChecker.CheckProperties(this);
        
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		var firstScene = Root.Instance.PackedScenes.Screen.FirstScene;
		MenuContainer.ChangeStoredNode(firstScene.Instantiate() as Control);
	}
}