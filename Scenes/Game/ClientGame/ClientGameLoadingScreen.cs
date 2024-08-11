using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	private static readonly PackedScene DefaultLoadingScreen = ClientRoot.Instance.PackedScenes.Client.Screens.WaitingConnectionCanvas;

	public CanvasLayer LoadingCanvas { get; private set; }

	public void SetDefaultLoadingScreen()
	{
		SetLoadingScreen(DefaultLoadingScreen.Instantiate<CanvasLayer>());
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
