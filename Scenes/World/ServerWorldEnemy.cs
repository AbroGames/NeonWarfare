using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld
{

    public IReadOnlyList<ServerEnemy> Enemies => _enemies;
    private List<ServerEnemy> _enemies = new();

    public ServerEnemy SpawnEnemy(EnemyInfoStorage.EnemyType type, Vector2 position, float rotation)
    {
        ServerEnemy enemy = CreateNetworkEntity<ServerEnemy>(EnemyInfoStorage.GetServerScene(type));
        enemy.InitComponents();
        enemy.InitOnSpawn(position, rotation);
        enemy.InitStats(EnemyInfoStorage.GetEnemyInfo(type));
        
        AddChild(enemy);
        _enemies.Add(enemy);
        enemy.TreeExiting += () => RemoveEnemy(enemy);
        
        //У всех игроков спауним нового врага
        Network.SendToAll(new ClientWorld.SC_EnemySpawnPacket(type, enemy.Nid, enemy.Position, enemy.Rotation));

        return enemy;
    }
    
    public void RemoveEnemy(ServerEnemy enemy)
    {
        _enemies.Remove(enemy);
    }
}
