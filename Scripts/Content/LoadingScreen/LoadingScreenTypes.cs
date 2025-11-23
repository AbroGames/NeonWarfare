using System.Collections.Generic;

namespace NeonWarfare.Scripts.Content.LoadingScreen;

public static class LoadingScreenTypes
{
    public enum Type
    {
        Connecting, 
        WaitingSyncing, 
        Loading
    }

    private static readonly Dictionary<Type, string> LoadingScreenTextByType = new()
    {
        { Type.Connecting, "Connecting to server" },
        { Type.Loading, "Loading" },
        { Type.WaitingSyncing, "Wait for the synchronization ends" }
    };

    public static string GetLoadingScreenText(Type loadingScreenType)
    {
        return LoadingScreenTextByType[loadingScreenType];
    }
}