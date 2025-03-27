using Godot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot
{
    [Export] [NotNull] private CanvasLayer LoadingScreenLayer { get; set; }
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
        LoadingScreenLayer.AddChild(loadingScreen);
    }
}