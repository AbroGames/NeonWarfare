using NeonWarfare.Scenes.Game.ServerGame;

namespace NeonWarfare.Scenes.Root.ServerRoot;

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
	
	public void CreateServerGame(int port, int? clientPid)
	{
		ServerGame serverGame = new ServerGame();
		if (clientPid.HasValue)
		{
			serverGame.AddClientDeadChecker(clientPid.Value);
		}
		
		SetMainScene(serverGame);
		serverGame.CreateServer(port);
	}
}
