using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scripts.Content.CmdArgs;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public class DedicatedServerRootStarter(RootData rootData) : BaseRootStarter(rootData)
{
    
    // The display server of a windowless run, see Docs/Cli-args.md.
    private const string HeadlessDisplayServer = "headless";
    
    private DedicatedServerArgs _dedicatedServerArgs;
    private readonly ILogger _log = LogFactory.GetForStatic<DedicatedServerRootStarter>();
    
    public override void Init()
    {
        base.Init();
        _log.Information("Initializing DedicatedServer...");
        
        _dedicatedServerArgs = DedicatedServerArgs.GetFromCmd(CmdArgsService);
        
        Services.SaveLoad.Init(true);
        Services.LastGame.Init();
        Services.DedicatedServerSettings.Init();
        
        // The locale is a setting, so it is set only after the settings are loaded.
        Services.I18N.SetCurrentLocale(Services.DedicatedServerSettings.GetSettings().Locale);
        
        RootData.SceneTree.Root.Title = $"[SERVER] {RootData.SceneTree.Root.Title}";
    }

    public override void Start()
    {
        _log.Information("Starting DedicatedServer...");

        Services.MainScene.HostMultiplayerGameAsDedicatedServer(
            _dedicatedServerArgs.SaveFileName ?? Services.SaveLoad.GenNewSaveFileName(),
            _dedicatedServerArgs.Port,
            _dedicatedServerArgs.Admin,
            _dedicatedServerArgs.ParentPid,
            DisplayServer.GetName() != HeadlessDisplayServer);
    }
}