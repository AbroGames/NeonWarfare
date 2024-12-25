using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientRoot
{
	
	public ClientGame Game { get; private set; }
	public MainMenuMainScene MainMenu { get; private set; }

	private void SetMainScene(ClientGame game)
	{
		ClearStoredNode();
		Game = game;
		AddChild(game);
	}
	
	private void SetMainScene(MainMenuMainScene mainScene)
	{
		ClearStoredNode();
		MainMenu = mainScene;
		AddChild(mainScene);
	}

	private void ClearStoredNode()
	{
		Game?.QueueFree();
		Game = null;
		
		MainMenu?.QueueFree();
		MainMenu = null;
	}
	
	public void CreateClientGame(string host, int port, int? serverPid = null)
	{
		ClientGame clientGame = new ClientGame();
		if (serverPid.HasValue)
		{
			clientGame.AddServerShutdowner(serverPid.Value);
		}
        
		SetMainScene(clientGame);
		clientGame.ConnectToServer(host, port);
	}
	
	public void CreateMainMenu()
	{
		var mainMenu = PackedScenes.MainMenuPackedScene;
		SetMainScene(mainMenu.Instantiate<MainMenuMainScene>());
	}
}