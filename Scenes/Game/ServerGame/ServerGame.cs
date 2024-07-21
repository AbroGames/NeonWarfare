using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ServerGame : Node2D
{
	
	public WorldMainScene MainScene { get; private set; }
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		var safeWorld = ServerRoot.Instance.PackedScenes.Main.SafeWorld;
		ChangeMainScene(safeWorld.Instantiate<SafeWorldMainScene>());
	}
	
	public void ChangeMainScene(WorldMainScene worldMainScene)
	{
		MainScene?.QueueFree();
		MainScene = worldMainScene;
		AddChild(MainScene);
	}
}
