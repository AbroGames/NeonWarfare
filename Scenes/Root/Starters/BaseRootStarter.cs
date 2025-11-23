using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Logging;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public abstract class BaseRootStarter
{
    
    [Logger] private ILogger _log;

    public virtual void Init(Root root)
    {
        Di.Process(this);
        
        LogFactory.GodotPushEnable = Services.CmdArgs.GodotLogPush;
        Services.ExceptionHandler.AddExceptionHandlerForUnhandledException();
        Services.CmdArgs.LogCmdArgs();
        
        _log.Information("Initializing base...");
        Services.Net.Init(root, Services.CmdArgs.IsDedicatedServer);
        Services.LoadingScreen.Init(root.LoadingScreenContainer, root.PackedScenes.LoadingScreen);
        Services.MainScene.Init(root.MainSceneContainer, root.PackedScenes.Game, root.PackedScenes.MainMenu);
    }

    public virtual void Start(Root root)
    {
        _log.Information("Starting base...");
    }
}