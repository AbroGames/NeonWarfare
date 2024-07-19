using Godot;
using KludgeBox;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ClientRoot
{
	
	public ClientGame Game { get; private set; }
	public MainMenuMainScene MainMenu { get; private set; }

	public void SetMainScene(ClientGame game)
	{
		ClearStoredNode();
		Game = game;
		AddChild(game);
	}
	
	public void SetMainScene(MainMenuMainScene mainScene)
	{
		ClearStoredNode();
		MainMenu = mainScene;
		AddChild(mainScene);
	}

	public void ClearStoredNode()
	{
		Game?.QueueFree();
		Game = null;
		
		MainMenu?.QueueFree();
		MainMenu = null;
	}
}