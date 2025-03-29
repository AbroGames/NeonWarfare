using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces;
using NeonWarfare.Scenes.Screen.MainScene;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot
{
	[Export] [NotNull] private Node2D ContextContainer { get; set; }
	public ClientGame Game { get; private set; }
	public MainMenuMainScene MainMenu { get; private set; }

	private void SetMainScene(ClientGame game)
	{
		ClearStoredNode();
		Game = game;
		ContextContainer.AddChild(game);
	}
	
	private void SetMainScene(MainMenuMainScene mainScene)
	{
		ClearStoredNode();
		MainMenu = mainScene;
		ContextContainer.AddChild(mainScene);
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
		var mainMenuScene = PackedScenes.MainMenuPackedScene;
		var mainMenu = mainMenuScene.Instantiate<MainMenuMainScene>();
		SetMainScene(mainMenu);
	}
}
