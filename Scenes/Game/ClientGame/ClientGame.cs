using Godot;
using KludgeBox;
using NeonWarfare.NetOld;

public partial class ClientGame : Node2D
{
	
	public WorldMainScene MainScene { get; private set; }
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}
	
	public void ChangeMainScene(WorldMainScene worldMainScene)
	{
		MainScene?.QueueFree();
		MainScene = worldMainScene;
		AddChild(MainScene);
	}
}
