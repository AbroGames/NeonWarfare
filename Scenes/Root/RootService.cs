using Godot;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Utils;

namespace NeonWarfare;

public static class RootService
{
    public static void CommonInit(SceneMultiplayer sceneMultiplayer)
    {
        ExceptionHandlerService.AddExceptionHandlerForUnhandledException();
        CmdArgsService.LogCmdArgs();
        EventBus.Init();
        Netplay.Initialize(sceneMultiplayer);
    }
}