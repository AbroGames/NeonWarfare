using System.Linq;
using System.Reflection;
using KludgeBox.Core;
using KludgeBox.Logging;
using NeonWarfare.Scripts.Content.CmdArgs;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public abstract class BaseRootStarter
{
    
    /// <summary>
    /// Not in <c>Services</c>, because only a root starter reads the command line, see Docs/Cli-args.md.
    /// </summary>
    protected readonly CmdArgsService CmdArgsService = new();
    
    private CommonArgs _commonArgs;
    private readonly ILogger _log = LogFactory.GetForStatic<BaseRootStarter>();

    public virtual void Init(RootData rootData)
    {
        _commonArgs = CommonArgs.GetFromCmd(CmdArgsService);
        
        LogFactory.GodotPushEnable = _commonArgs.GodotLogPush;
        Services.ExceptionHandler.AddExceptionHandlerForUnhandledException();
        CmdArgsService.LogCmdArgs();
        
        _log.Information("Initializing base...");
        Services.AssemblyCache.AddAssembly(Assembly.GetExecutingAssembly());
        Services.TypesMapping.SetTypes(Services.AssemblyCache.GetTypes(Assembly.GetExecutingAssembly()).ToList());
        Services.LoadingScreen.Init(rootData.LoadingScreenContainer, rootData.PackedScenes.LoadingScreen);
        Services.MainScene.Init(
            rootData.MainSceneContainer, rootData.PackedScenes.Game, rootData.PackedScenes.MainMenu);
        Services.QuitRequests.Init(rootData.SceneTree);
        Services.I18N.Init(rootData.SceneTree);
    }

    public virtual void Start(RootData rootData)
    {
        _log.Information("Starting base...");
    }
}