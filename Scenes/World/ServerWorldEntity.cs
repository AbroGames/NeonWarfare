using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;
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
        player.InitOnProfile(playerProfile);
        AddChild(player);
        
        //У нового игрока спауним его самого
        Network.SendToClient(playerProfile.PeerId, 
            new ClientWorld.SC_PlayerSpawnPacket(player.Nid, player.Position.X, player.Position.Y, player.Rotation));
        //У всех остальных игроков спауним нового игрока
        Network.SendToAllExclude(playerProfile.PeerId, 
            new ClientWorld.SC_AllySpawnPacket(player.Nid, player.Position.X, player.Position.Y, player.Rotation, playerProfile.PeerId));
        
        return player;
    }

    public ServerEnemy SpawnEnemy(Vector2 position, float rotation)
    {
        ServerEnemy enemy = CreateNetworkEntity<ServerEnemy>(ServerRoot.Instance.PackedScenes.Enemy);
        enemy.AddChild(new ServerEnemyMovementComponent());
        enemy.AddChild(new ServerEnemyRotateComponent());
        enemy.Position = position;
        enemy.Rotation = rotation;
        AddChild(enemy);
        
        //У всех игроков спауним нового врага
        Network.SendToAll(new ClientWorld.SC_EnemySpawnPacket(enemy.Nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation));

        return enemy;
    }
    
    //TODO del after test
    public ServerBullet SpawnBullet(Vector2 position, float rotation, Color color)
    {
        ServerBullet bullet = CreateNetworkEntity<ServerBullet>(ServerRoot.Instance.PackedScenes.Bullet);
        bullet.Position = position;
        bullet.Rotation = rotation;
        AddChild(bullet);
        
        //У всех игроков спауним нового врага
        Network.SendToAll(new ClientWorld.SC_BulletSpawnPacket(bullet.GetChild<NetworkEntityComponent>().Nid, bullet.Position, bullet.Rotation, color));
        
        return bullet;
    }
}
