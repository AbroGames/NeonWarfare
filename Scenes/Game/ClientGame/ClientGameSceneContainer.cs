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
	
	public void ChangeMainScene(IWorldMainScene worldMainScene)
	{
		_mainScene?.QueueFree();
		_mainScene = worldMainScene.GetAsNode2D();
		AddChild(_mainScene);
		
		World = worldMainScene.GetWorld();
		Hud = worldMainScene.GetHud();
	}
}
