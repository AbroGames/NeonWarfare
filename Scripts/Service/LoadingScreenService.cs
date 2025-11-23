using Godot;
using NeonWarfare.Scripts.Content.LoadingScreen;
using LoadingScreenNode = NeonWarfare.Scenes.Screen.LoadingScreen.LoadingScreen;

namespace NeonWarfare.Scripts.Service;

public class LoadingScreenService
{
    
    private Scenes.KludgeBox.NodeContainer _loadingScreenContainer;
    private PackedScene _loadingScreenPackedScene;

    public void Init(Scenes.KludgeBox.NodeContainer loadingScreenContainer, PackedScene loadingScreenPackedScene)
    {
        _loadingScreenContainer = loadingScreenContainer;
        _loadingScreenPackedScene = loadingScreenPackedScene;
    }
    
    public LoadingScreenNode SetLoadingScreen(string text)
    {
        LoadingScreenNode loadingScreen = _loadingScreenPackedScene.Instantiate<LoadingScreenNode>().InitPreReady();
        if (text != null)
        {
            loadingScreen.SetText(text);
        }

        _loadingScreenContainer.ChangeStoredNode(loadingScreen);
        return loadingScreen;
    }
    
    public LoadingScreenNode SetLoadingScreen(LoadingScreenTypes.Type loadingScreenType)
    {
        return SetLoadingScreen(LoadingScreenTypes.GetLoadingScreenText(loadingScreenType));
    }
	
    public void Clear()
    {
        _loadingScreenContainer.ClearStoredNode();
    }
}