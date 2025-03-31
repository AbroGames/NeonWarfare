using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld
{
    
    public IReadOnlyList<ServerPlayer> Players => _players;
    public IReadOnlyDictionary<long, ServerPlayer> PlayersByPeerId => _playersByPeerId;
    
    private List<ServerPlayer> _players = new(); 
    private Dictionary<long, ServerPlayer> _playersByPeerId = new();
    
    public IReadOnlyDictionary<long, ServerPlayer> GetPlayerProfilesByPeerIdExcluding(long excludePeerId)
    {
        return _playersByPeerId
            .Where(kv => kv.Key != excludePeerId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ServerPlayer> GetPlayerProfilesExcluding(long excludePeerId)
    {
        return GetPlayerProfilesByPeerIdExcluding(excludePeerId).Values;
    }

    public ServerPlayer SpawnPlayerInCenter(ServerPlayerProfile playerProfile)
    {
        return SpawnPlayer(
            playerProfile,
            Vec(Rand.Range(-150, 150), Rand.Range(-150, 150)),
            Mathf.DegToRad(Rand.Range(0, 360))
            );
    }
    
    public ServerPlayer SpawnPlayer(ServerPlayerProfile playerProfile, Vector2 position, float rotation) 
    {
        ServerPlayer player = CreateNetworkEntity<ServerPlayer>(ServerRoot.Instance.PackedScenes.Player);
        player.Position = position;
        player.Rotation = rotation;
        player.AddChild(new NetworkInertiaComponent());
        player.TreeExiting += () => RemovePlayer(player);
        
        player.InitOnProfile(playerProfile);
        AddChild(player);
        _players.Add(player);
        _playersByPeerId.Add(playerProfile.PeerId, player);
        
        //У нового игрока спауним его самого
        Network.SendToClient(playerProfile.PeerId, 
            new ClientWorld.SC_PlayerSpawnPacket(player.Nid, player.Position, player.Rotation, playerProfile.Color));
        //У всех остальных игроков спауним нового игрока
        Network.SendToAllExclude(playerProfile.PeerId, 
            new ClientWorld.SC_AllySpawnPacket(player.Nid, player.Position, player.Rotation, playerProfile.Color, playerProfile.PeerId));
        
        return player;
    }

    public void RemovePlayer(ServerPlayer player)
    {
        _players.Remove(player);
        _playersByPeerId.Remove(player.PlayerProfile.PeerId);
    }
    
    public void InitPlayers(List<ServerPlayerProfile> playerProfiles)
    {
        foreach (ServerPlayerProfile playerProfile in playerProfiles)
        {
            SpawnPlayerInCenter(playerProfile);
        }
    }

    public void CheckAllDeadAndRestart()
    {
        //Если все игроки мертвы, то загружает SafeWorld
        if (Players.Count > 0 && Players.Where(p => !p.IsDead).Count() == 0)
        {
            //TODO Здесь и в NeonWarfare.Scenes.Game.ServerGame.ServerGame.OnWantToBattlePacket много дубоирования, вынести в Game/World
            Network.SendToAll(new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
            Network.SendToAll(new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe));

            ServerSafeWorld safeWorld = ServerRoot.Instance.PackedScenes.SafeWorld.Instantiate<ServerSafeWorld>();
            ServerRoot.Instance.Game.ChangeMainScene(safeWorld);
            safeWorld.Init(ServerRoot.Instance.Game.PlayerProfiles.ToList());
        
            Network.SendToAll(new ClientGame.SC_ClearLoadingScreenPacket());
        }
    }
    
}
