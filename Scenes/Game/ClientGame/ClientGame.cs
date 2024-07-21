using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame : Node2D
{
	
	public World World { get; private set; }
	public Hud Hud { get; private set; }
	private Node2D _mainScene;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}
	
	public void ChangeMainScene(IWorldMainScene worldMainScene)
	{
		_mainScene?.QueueFree();
		_mainScene = worldMainScene.GetAsNode2D();
		AddChild(_mainScene);
		
		World = worldMainScene.GetWorld();
		Hud = worldMainScene.GetHud();
	}
}
