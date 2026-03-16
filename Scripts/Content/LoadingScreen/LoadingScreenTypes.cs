using System.Collections.Generic;

namespace NeonWarfare.Scripts.Content.LoadingScreen;

public static class LoadingScreenTypes
{
    public enum Type
    {
        Loading,
        Connecting, 
        WaitingSyncing
    }

    private static readonly Dictionary<Type, string> LoadingScreenTextKeyByType = new()
    {
        { Type.Loading, "LOADING_SCREEN__LOADING" },
        { Type.Connecting, "LOADING_SCREEN__CONNECTING" },
        { Type.WaitingSyncing, "LOADING_SCREEN__WAITING_SYNCING" }
    };

    public static string GetLoadingScreenText(Type loadingScreenType)
    {
        return Services.I18N.Tr(LoadingScreenTextKeyByType[loadingScreenType]);
    }
}