using Godot;
using KludgeBox;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ServerRoot
{
	
	public ServerGame Game { get; private set; }

	private void SetMainScene(ServerGame game)
	{
		ClearStoredNode();
		Game = game;
		AddChild(game);
	}

	private void ClearStoredNode()
	{
		Game?.QueueFree();
		Game = null;
	}
	
	public void CreateServerGame(int port)
	{
		ServerGame serverGame = new ServerGame();
		SetMainScene(serverGame);
		serverGame.CreateServer(port);
	}
}