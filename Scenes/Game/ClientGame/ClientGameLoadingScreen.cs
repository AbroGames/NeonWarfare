using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.LoadingScreen;

public partial class ClientGame
{
	public CanvasLayer LoadingCanvas { get; private set; }

	public void SetDefaultLoadingScreen()
	{
		SetLoadingScreen(LoadingScreen.Create("Connecting to server"));
	}
	
	public void SetLoadingScreen(CanvasLayer loadingScreen)
	{
		LoadingCanvas?.QueueFree();
		LoadingCanvas = loadingScreen;
		AddChild(loadingScreen);
		MoveChild(loadingScreen, 0);
	}
	
	public void ClearLoadingScreen()
	{
		LoadingCanvas?.QueueFree();
	}
	
}
