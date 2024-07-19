using Godot;
using KludgeBox;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ServerRoot
{
	
	public ServerGame Game { get; private set; }

	public void SetMainScene(ServerGame game)
	{
		ClearStoredNode();
		Game = game;
		AddChild(game);
	}

	public void ClearStoredNode()
	{
		Game?.QueueFree();
		Game = null;
	}
}