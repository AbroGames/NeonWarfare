using System.Collections.Generic;

namespace NeonWarfare.LoadingScreen;

public class LoadingScreenBuilder
{
    public enum LoadingScreenType
    {
        CONNECTING, WAITING_END_OF_BATTLE, LOADING
    }
    
    private static Dictionary<LoadingScreenType, string> _loadingScreenTextByType = new()
    {
        { LoadingScreenType.CONNECTING, "Connecting to server" },
        { LoadingScreenType.LOADING, "Loading" },
        { LoadingScreenType.WAITING_END_OF_BATTLE, "Wait for the end of the battle" },
    };

    public static LoadingScreen Create(string text = null)
    {
        LoadingScreen loadingScreen = ClientRoot.Instance.PackedScenes.LoadingScreenCanvas.Instantiate<LoadingScreen>();
        if (text != null)
        {
            loadingScreen.SetUpperText(text);
        }

        return loadingScreen;
    }
    
    public static LoadingScreen Create(LoadingScreenType loadingScreenType)
    {
        return Create(_loadingScreenTextByType[loadingScreenType]);
    }
}