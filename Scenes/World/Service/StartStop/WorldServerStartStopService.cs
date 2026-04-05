using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Service.Command;
using NeonWarfare.Scenes.World.Tree;
using Serilog;

namespace NeonWarfare.Scenes.World.Service.StartStop;


public partial class WorldServerStartStopService : Node
{
    
    [Parent] private World _world;
    
    [SceneService] private WorldTree _tree;
    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldTemporaryData _temporaryData;
    
    [SceneService] private WorldDataSaveLoadService _dataSaveLoadService;
    [SceneService] private WorldCommandService _commandService;
    
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void StartNewGame(string adminNickname = null)
    {
        if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
        
        CommonServerInit(adminNickname);
        NewGameServerInit();
        EndCommonServerInit();
    }
    
    public void LoadGame(string saveFileName, string adminNickname = null)
    {
        if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
        
        CommonServerInit(adminNickname);
        LoadServerInit(saveFileName);
        EndCommonServerInit();
    }

    private void CommonServerInit(string adminNickname = null)
    {
        _log.Information("World starting...");
        
        //Init WorldTemporaryData
        _temporaryData.MainAdminNick = adminNickname;
        // Use inner function for detach this function after server shutdown,
        // otherwise we can have memory leak for this function
        void PeerDisconnectedEvent(long id)
        {
            _temporaryData.PlayerNickByPeerId.Remove((int) id);
        }
        GetMultiplayer().PeerDisconnected += PeerDisconnectedEvent;
        
        //Init command system
        _commandService.InitOnServer();
        
        //Init node for server shutdown process in the future
        WorldServerShutdowner worldServerShutdowner = new WorldServerShutdowner();
        worldServerShutdowner.AddCustomShutdownAction(() => GetMultiplayer().PeerDisconnected -= PeerDisconnectedEvent);
        AddChild(worldServerShutdowner);
    }

    private void NewGameServerInit()
    {
        
    }

    private void LoadServerInit(string saveFileName)
    {
        _dataSaveLoadService.Load(saveFileName);
    }

    private void EndCommonServerInit()
    {
        _tree.SetSafeSurface();
        _tree.Surface.InitOnServer();//TODO временно для теста
    }
}