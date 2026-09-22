using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.OldWorld.Data.PersistenceData;
using NeonWarfare.Scenes.OldWorld.Data.TemporaryData;
using NeonWarfare.Scenes.OldWorld.Tree;
using NeonWarfare.Scenes.OldWorld.WorldServices.Command;
using Serilog;

namespace NeonWarfare.Scenes.OldWorld.WorldServices.StartStop;


public partial class WorldServerStartStopService : Node
{
    
    [Parent] private World _world;
    
    [SceneService] private WorldTree _tree;
    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldTemporaryData _temporaryData;
    
    [SceneService] private WorldSynchronizerService _synchronizerService;
    [SceneService] private WorldDataSaveLoadService _dataSaveLoadService;
    [SceneService] private WorldCommandService _commandService;
    
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void StartNewGame(string saveFileName, string adminUid)
    {
        if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
        
        CommonServerInit(adminUid);
        NewGameServerInit(saveFileName);
        EndCommonServerInit();
    }
    
    public void LoadGame(string saveFileName, string adminUid)
    {
        if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
        
        CommonServerInit(adminUid);
        LoadServerInit(saveFileName);
        EndCommonServerInit();
    }

    private void CommonServerInit(string adminUid)
    {
        _log.Information("World starting...");
        
        // Init WorldTemporaryData
        GetMultiplayer().PeerDisconnected += id => _temporaryData.PlayerUidByPeerId.Remove((int) id);
        
        // Init WorldSynchronizerService
        _synchronizerService.InitOnServer(adminUid);
        
        // Init command system
        _commandService.InitOnServer();
        
        // Init node for server shutdown process in the future
        AddChild(new WorldServerShutdowner());
    }

    private void NewGameServerInit(string saveFileName)
    {
        // Set savaFileName for future saving or auto-saving
        _persistenceData.General.GeneralData.SaveFileName = saveFileName;
    }

    private void LoadServerInit(string saveFileName)
    {
        _dataSaveLoadService.Load(saveFileName);
    }

    private void EndCommonServerInit()
    {
        _tree.SetSafeSurface();
    }
}