using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Screen.LoadingScreen;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame
{
	public CanvasLayer LoadingCanvas { get; private set; }
	
	public void SetLoadingScreen(LoadingScreenBuilder.LoadingScreenType loadingScreenType)
	{
		SetLoadingScreen(LoadingScreenBuilder.Create(loadingScreenType));
	}
	
	public void ClearLoadingScreen()
	{
		LoadingCanvas?.QueueFree();
		LoadingCanvas = null;
	}
	
	private void SetLoadingScreen(CanvasLayer loadingScreen)
	{
		LoadingCanvas?.QueueFree();
		LoadingCanvas = loadingScreen;
		AddChild(loadingScreen);
		MoveChild(loadingScreen, -1);
	}
}
