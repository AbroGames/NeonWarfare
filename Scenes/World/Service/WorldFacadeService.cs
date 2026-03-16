using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.PersistenceData.Player;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Scenes.ClientScenes;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Service.Chat;
using NeonWarfare.Scenes.World.Service.Command;
using NeonWarfare.Scenes.World.Service.DataSerializer;
using NeonWarfare.Scenes.World.Service.Performance;
using NeonWarfare.Scenes.World.Service.PersistenceFactory;
using NeonWarfare.Scenes.World.Service.StartStop;
using NeonWarfare.Scenes.World.Tree;
using KludgeBox.DI.Requests.SceneServiceInjection;

namespace NeonWarfare.Scenes.World.Service;

public partial class WorldFacadeService : Node
{
    
    [SceneService] private WorldTree _tree;
    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldTemporaryData _temporaryData;
    
    [SceneService] private PersistenceNodesFactoryService _factoryService;
    [SceneService] private WorldMultiplayerSpawnerService _multiplayerSpawnerService;
    [SceneService] private WorldServerStartStopService _serverStartStopService;
    [SceneService] private WorldClientStartStopService _clientStartStopService;
    [SceneService] private WorldSynchronizerService _synchronizerService;
    [SceneService] private WorldDataSaveLoadService _dataSaveLoadService;
    [SceneService] private WorldDataSerializerService _dataSerializerService;
    [SceneService] private WorldPerformanceService _performanceService;
    [SceneService] private WorldChatService _chatService;
    [SceneService] private WorldCommandService _commandService;
    
    [SceneService] private SyncedPackedScenes _syncedPackedScenes;
    [SceneService] private ClientPackedScenes _clientPackedScenes;
    
    public override void _Ready()
    {
        Di.Process(this);
    }

    public PlayerData GetClientPlayerData()
    {
        return GetPlayerData(GetMultiplayer().GetUniqueId());
    }

    public PlayerData GetPlayerData(long peerId)
    {
        String playerNick = _temporaryData.PlayerNickByPeerId.GetValueOrDefault(peerId, null);
        if (playerNick == null) return null;
        
        return _persistenceData.Players.PlayerByNick.GetValueOrDefault(playerNick, null);
    }

    public List<PlayerData> GetOnlinePlayers()
    {
        return _persistenceData.Players.PlayerByNick.Values
            .Where(playerData => _temporaryData.PlayerNickByPeerId.Values.Contains(playerData.Nick))
            .ToList();
    }
    
    public List<PlayerData> GetOfflinePlayers()
    {
        return _persistenceData.Players.PlayerByNick.Values
            .Where(playerData => !_temporaryData.PlayerNickByPeerId.Values.Contains(playerData.Nick))
            .ToList();
    }

    public bool IsAdmin(long peerId)
    {
        if (peerId == ServerId) return true;
        
        PlayerData playerData = GetPlayerData(peerId);
        if (playerData == null) return false;
        return playerData.IsAdmin;
    }
}