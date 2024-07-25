using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ServerGame : Node2D
{
	
	public World World { get; private set; }
	private Node2D _mainScene;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		var safeWorld = ServerRoot.Instance.PackedScenes.Main.SafeWorld;
		ChangeMainScene(safeWorld.Instantiate<SafeGameMainScene>());
	}
	
	public void ChangeMainScene(IGameMainScene gameMainScene)
	{
		_mainScene?.QueueFree();
		_mainScene = gameMainScene.GetAsNode2D();
		AddChild(_mainScene);
		
		World = gameMainScene.GetWorld();
	}
}
