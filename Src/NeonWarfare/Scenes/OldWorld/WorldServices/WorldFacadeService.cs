using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.OldWorld.Data.PersistenceData;
using NeonWarfare.Scenes.OldWorld.Data.PersistenceData.Player;
using NeonWarfare.Scenes.OldWorld.Data.TemporaryData;
using NeonWarfare.Scenes.OldWorld.Scenes.ClientScenes;
using NeonWarfare.Scenes.OldWorld.Scenes.SyncedScenes;
using NeonWarfare.Scenes.OldWorld.Tree;
using NeonWarfare.Scenes.OldWorld.WorldServices.Characters;
using NeonWarfare.Scenes.OldWorld.WorldServices.Chat;
using NeonWarfare.Scenes.OldWorld.WorldServices.Command;
using NeonWarfare.Scenes.OldWorld.WorldServices.DataSerializer;
using NeonWarfare.Scenes.OldWorld.WorldServices.Performance;
using NeonWarfare.Scenes.OldWorld.WorldServices.StartStop;

namespace NeonWarfare.Scenes.OldWorld.WorldServices;

public partial class WorldFacadeService : Node
{
    
    [SceneService] private WorldTree _tree;
    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldTemporaryData _temporaryData;
    
    [SceneService] private WorldMultiplayerSpawnerService _multiplayerSpawnerService;
    [SceneService] private WorldServerStartStopService _serverStartStopService;
    [SceneService] private WorldClientStartStopService _clientStartStopService;
    [SceneService] private WorldSynchronizerService _synchronizerService;
    [SceneService] private WorldDataSaveLoadService _dataSaveLoadService;
    [SceneService] private WorldDataSerializerService _dataSerializerService;
    [SceneService] private WorldPerformanceService _performanceService;
    [SceneService] private WorldChatService _chatService;
    [SceneService] private WorldCommandService _commandService;
    
    [SceneService] private WorldPlayerService _playerService;
    [SceneService] private WorldEnemyService _enemyService;
    
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
        String playerUid = _temporaryData.PlayerUidByPeerId.GetValueOrDefault(peerId, null);
        if (playerUid == null) return null;
        
        return _persistenceData.Players.PlayerByUid.GetValueOrDefault(playerUid, null);
    }

    public List<PlayerData> GetOnlinePlayers()
    {
        return _persistenceData.Players.PlayerByUid.Values
            .Where(playerData => _temporaryData.PlayerUidByPeerId.Values.Contains(playerData.Uid))
            .ToList();
    }
    
    public List<PlayerData> GetOfflinePlayers()
    {
        return _persistenceData.Players.PlayerByUid.Values
            .Where(playerData => !_temporaryData.PlayerUidByPeerId.Values.Contains(playerData.Uid))
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