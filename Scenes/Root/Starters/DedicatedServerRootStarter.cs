using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public class DedicatedServerRootStarter : BaseRootStarter
{
    
    [Logger] private ILogger _log;
    
    public override void Init(Root root)
    {
        base.Init(root);
        _log.Information("Initializing DedicatedServer...");
        
        root.GetTree().Root.Title = $"[SERVER] {root.GetTree().Root.Title}";
    }

    public override void Start(Root root)
    {
        base.Start(root);
        _log.Information("Starting DedicatedServer...");

        Services.MainScene.HostMultiplayerGameAsDedicatedServer(
            Services.CmdArgs.DedicatedServer.Port,
            Services.CmdArgs.DedicatedServer.SaveFileName,
            Services.CmdArgs.DedicatedServer.Admin,
            Services.CmdArgs.DedicatedServer.ParentPid,
            Services.CmdArgs.DedicatedServer.IsRender);
    }
}