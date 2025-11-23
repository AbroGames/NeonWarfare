using Godot;
using KludgeBox.DI.Requests.ParentInjection;

namespace NeonWarfare.Scenes.World.Services.StartStop;


public partial class WorldStartStopService : Node
{
    
    private readonly WorldTreeLoadService _worldTreeLoadService = new();
    [Parent] private World _world;

    /// <summary>
    /// In shutdown process (<see cref="Node.NotificationExitTree"/>) we can't use <c>Node.GetMultiplayer().IsServer()</c> for checking, because after TreeExit <c>Node.GetMultiplayer()</c> returns null.<br/>
    /// Also, <c>Game.Network</c> may have been disabled earlier, in which case it replaces the <c>Peer</c> with an <c>OfflinePeer</c> during the shutdown process, therefore we cannot trust <c>Game.Network</c>.
    /// </summary>
    private bool _isServer;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void StartNewGame(string adminNickname = null)
    {
        ServerInit(adminNickname);
    }
    
    public void LoadGame(string saveFileName, string adminNickname = null)
    {
        ServerInit(adminNickname);
        _world.Data.SaveLoad.Load(saveFileName);
        _worldTreeLoadService.RunAllLoaders(_world);
    }

    private void ServerInit(string adminNickname = null)
    {
        _isServer = true;
        _world.TemporaryDataService.InitOnServer(adminNickname);
    }
    
    public override void _Notification(int id)
    {
        if (id == NotificationExitTree && _isServer) Shutdown();
    }
    
    private void Shutdown()
    {
        _world.Data.SaveLoad.AutoSave();
    }
}