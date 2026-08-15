using System;
using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;
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
    
    public LoadingScreen SetLoadingScreen(string text, Action cancelAction = null)
    {
        LoadingScreen loadingScreen = _loadingScreenPackedScene.Instantiate<LoadingScreen>().InitPreReady();
        if (text != null)
        {
            loadingScreen.SetText(text);
        }

        loadingScreen.SetCancelAction(cancelAction);
        _loadingScreenContainer.ChangeStoredNode(loadingScreen);
        return loadingScreen;
    }
    
    public LoadingScreen SetLoadingScreen(LoadingScreenTypes.Type loadingScreenType, Action cancelAction = null)
    {
        return SetLoadingScreen(LoadingScreenTypes.GetLoadingScreenText(loadingScreenType), cancelAction);
    }
	
    public void Clear()
    {
        _loadingScreenContainer.ClearStoredNode();
    }
}