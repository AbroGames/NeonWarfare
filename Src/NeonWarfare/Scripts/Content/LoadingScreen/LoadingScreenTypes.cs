using System.Collections.Generic;

namespace NeonWarfare.Scripts.Content.LoadingScreen;

public static class LoadingScreenTypes
{
    public enum Type
    {
        Loading,
        Connecting, 
        StoppingServer
    }

    private static readonly Dictionary<Type, string> LoadingScreenTextKeyByType = new()
    {
        { Type.Loading, "LOADING_SCREEN__LOADING" },
        { Type.Connecting, "LOADING_SCREEN__CONNECTING" },
        { Type.StoppingServer, "LOADING_SCREEN__STOPPING_SERVER" }
    };

    public static string GetLoadingScreenText(Type loadingScreenType)
    {
        return Services.I18N.Tr(LoadingScreenTextKeyByType[loadingScreenType]);
    }
}