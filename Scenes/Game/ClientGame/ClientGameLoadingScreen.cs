using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	private static readonly PackedScene DefaultLoadingScreen = ClientRoot.Instance.PackedScenes.Client.Screens.WaitingConnectionCanvas;

	public CanvasLayer LoadingCanvas { get; private set; }

	public override void _Ready()
	{
		SetLoadingScreen(DefaultLoadingScreen.Instantiate<CanvasLayer>());
	}
	
	//TODO перенести сюда Network, а так же ServerShutdowner
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
