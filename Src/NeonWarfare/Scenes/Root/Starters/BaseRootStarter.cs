using System.Linq;
using System.Reflection;
using KludgeBox.Core;
using KludgeBox.Logging;
using NeonWarfare.Scripts.Content.CmdArgs;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public abstract class BaseRootStarter(RootData rootData)
{
    
    protected readonly RootData RootData = rootData;
    
    /// <summary>
    /// Not in <c>Services</c>, because only a root starter reads the command line, see Docs/Cli-args.md.
    /// </summary>
    protected readonly CmdArgsService CmdArgsService = new();
    
    private CommonArgs _commonArgs;
    private readonly ILogger _log = LogFactory.GetForStatic<BaseRootStarter>();

    public virtual void Init()
    {
        _commonArgs = CommonArgs.GetFromCmd(CmdArgsService);
        
        LogFactory.GodotPushEnable = _commonArgs.GodotLogPush;
        Services.ExceptionHandler.AddExceptionHandlerForUnhandledException();
        CmdArgsService.LogCmdArgs();
        
        _log.Information("Initializing base...");
        Services.AssemblyCache.AddAssembly(Assembly.GetExecutingAssembly());
        Services.TypesMapping.SetTypes(Services.AssemblyCache.GetTypes(Assembly.GetExecutingAssembly()).ToList());
        Services.LoadingScreen.Init(RootData.LoadingScreenContainer, RootData.PackedScenes.LoadingScreen);
        Services.MainScene.Init(
            RootData.MainSceneContainer, RootData.PackedScenes.Game, RootData.PackedScenes.MainMenu);
        Services.QuitRequests.Init(RootData.SceneTree);
        Services.I18N.Init(RootData.SceneTree);
    }

    public abstract void Start();
}