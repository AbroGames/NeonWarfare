using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame 
{
	
	public ClientWorld World { get; private set; }
	public Hud Hud { get; private set; }
	private Node2D _mainScene;
	
	public void ChangeMainScene(IGameMainScene gameMainScene)
	{
		_mainScene?.QueueFree();
		_mainScene = gameMainScene.GetAsNode2D();
		AddChild(_mainScene);
		
		World = gameMainScene.GetWorld();
		Hud = gameMainScene.GetHud();
	}
}
