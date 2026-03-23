using Godot;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scripts.Content.LoadingScreen;

namespace NeonWarfare.Scripts.Service;

public class LoadingScreenService
{
    
    private NodeContainer _loadingScreenContainer;
    private PackedScene _loadingScreenPackedScene;

    public void Init(NodeContainer loadingScreenContainer, PackedScene loadingScreenPackedScene)
    {
        _loadingScreenContainer = loadingScreenContainer;
        _loadingScreenPackedScene = loadingScreenPackedScene;
    }
    
    public LoadingScreen SetLoadingScreen(string text)
    {
        LoadingScreen loadingScreen = _loadingScreenPackedScene.Instantiate<LoadingScreen>().InitPreReady();
        if (text != null)
        {
            loadingScreen.SetText(text);
        }

        _loadingScreenContainer.ChangeStoredNode(loadingScreen);
        return loadingScreen;
    }
    
    public LoadingScreen SetLoadingScreen(LoadingScreenTypes.Type loadingScreenType)
    {
        return SetLoadingScreen(LoadingScreenTypes.GetLoadingScreenText(loadingScreenType));
    }
	
    public void Clear()
    {
        _loadingScreenContainer.ClearStoredNode();
    }
}