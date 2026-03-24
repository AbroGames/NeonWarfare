using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using Serilog;

namespace NeonWarfare.Scenes.World.Service.StartStop;

/// <summary>
/// In shutdown process (<see cref="Node.NotificationExitTree"/>) we can't use <c>Node.GetMultiplayer().IsServer()</c> for checking, because after TreeExit <c>Node.GetMultiplayer()</c> returns null.<br/>
/// Also, <c>Game.Network</c> may have been disabled earlier, in which case it replaces the <c>Peer</c> with an <c>OfflinePeer</c> during the shutdown process, therefore we cannot trust <c>Game.Network</c>.<br/>
/// So, the solution is using specific node, only on server, which will catch <see cref="Node.NotificationExitTree"/> and can run server shutdown process.
/// </summary>
public partial class WorldServerShutdowner : Node
{
    
    [SceneService] private WorldDataSaveLoadService _dataSaveLoadService;
    private Action _shutdownActions;
    
    [Logger] private ILogger _log;
    
    public override void _Ready()
    {
        Di.Process(this);
    }

    public void AddCustomShutdownAction(Action shutdownAction)
    {
        _shutdownActions += shutdownAction;
    }
    
    public override void _Notification(int id)
    {
        if (id == NotificationExitTree) ServerShutdown();
    }
    
    private void ServerShutdown()
    {
        _log.Information("World stoping...");
        _dataSaveLoadService.AutoSave();
        _shutdownActions?.Invoke();
    }
}