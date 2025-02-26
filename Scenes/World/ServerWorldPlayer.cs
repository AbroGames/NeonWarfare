using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
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
        
        ExitTreeLogicComponent exitTreeLogicComponent = new();
        exitTreeLogicComponent.AddActionWhenExitTree(node => RemovePlayer((ServerPlayer) node));
        player.AddChild(exitTreeLogicComponent);
        
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
    
}
