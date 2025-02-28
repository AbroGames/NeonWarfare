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

    public IReadOnlyList<ServerEnemy> Enemies => _enemies;
    private List<ServerEnemy> _enemies = new();

    public ServerEnemy SpawnEnemy(Vector2 position, float rotation)
    {
        ServerEnemy enemy = CreateNetworkEntity<ServerEnemy>(ServerRoot.Instance.PackedScenes.Enemy);
        enemy.Position = position;
        enemy.Rotation = rotation;
        enemy.AddChild(new ServerEnemyMovementComponent());
        enemy.AddChild(new ServerEnemyRotateComponent());
        enemy.TreeExiting += () => RemoveEnemy(enemy);
        
        AddChild(enemy);
        _enemies.Add(enemy);
        
        //У всех игроков спауним нового врага
        Network.SendToAll(new ClientWorld.SC_EnemySpawnPacket(enemy.Nid, enemy.Position, enemy.Rotation, new Color(1, 0, 0))); //TODO брать из сцены или из описания конкретного врага

        return enemy;
    }
    
    public void RemoveEnemy(ServerEnemy enemy)
    {
        _enemies.Remove(enemy);
    }
}
