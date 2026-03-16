using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scripts.Content.CmdArgs;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public class DedicatedServerRootStarter : BaseRootStarter
{
    
    private DedicatedServerArgs _dedicatedServerArgs;
    [Logger] private ILogger _log;
    
    public override void Init(RootData rootData)
    {
        base.Init(rootData);
        _log.Information("Initializing DedicatedServer...");
        
        _dedicatedServerArgs = DedicatedServerArgs.GetFromCmd(CmdArgsService);
        
        Services.Net.Init(rootData.SceneTree, true);
        rootData.SceneTree.Root.Title = $"[SERVER] {rootData.SceneTree.Root.Title}";
    }

    public override void Start(RootData rootData)
    {
        base.Start(rootData);
        _log.Information("Starting DedicatedServer...");

        Services.MainScene.HostMultiplayerGameAsDedicatedServer(
            _dedicatedServerArgs.Port,
            _dedicatedServerArgs.SaveFileName,
            _dedicatedServerArgs.Admin,
            _dedicatedServerArgs.ParentPid,
            _dedicatedServerArgs.IsRender);
    }
}