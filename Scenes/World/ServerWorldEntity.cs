using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld
{

    public ServerNetworkEntityManager NetworkEntityManager = new();
    
    public IEnumerable<ServerPlayer> GetPlayers()
    {
        return ServerRoot.Instance.Game.PlayerProfiles.Select(profile => profile.Player);
    }
    
    public ServerPlayer GetPlayerByPeerId(long peerId)
    {
        return ServerRoot.Instance.Game.PlayerProfilesByPeerId[peerId].Player;
    }
    
    public T CreateNetworkEntity<T>(PackedScene scene) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode);
        return newNode;
    }
    
    public ServerPlayer SpawnPlayer(ServerPlayerProfile playerProfile) 
    {
        ServerPlayer player = CreateNetworkEntity<ServerPlayer>(ServerRoot.Instance.PackedScenes.Player);
        player.AddChild(new NetworkInertiaComponent());
        player.InitOnProfile(playerProfile);
        AddChild(player);
        
        return player;
    }

    public ServerEnemy SpawnEnemy()
    {
        ServerEnemy enemy = CreateNetworkEntity<ServerEnemy>(ServerRoot.Instance.PackedScenes.Enemy);
        AddChild(enemy);

        return enemy;
    }
}
