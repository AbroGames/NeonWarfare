using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    public IReadOnlyList<ClientEnemy> Enemies => _enemies;
    private List<ClientEnemy> _enemies = new(); 
    
    [EventListener(ListenerSide.Client)]
    public void OnEnemySpawnPacket(SC_EnemySpawnPacket enemySpawnPacket)
    {
        ClientEnemy enemy = CreateNetworkEntity<ClientEnemy>(
            enemySpawnPacket.EnemyClientScene, enemySpawnPacket.Nid);
        enemy.InitComponents();
        enemy.InitOnSpawnPacket(enemySpawnPacket.Position, enemySpawnPacket.Rotation, enemySpawnPacket.Color);
        enemy.InitStats(EnemyInfoStorage.GetEnemyInfo(enemySpawnPacket.Type));
        
        AddChild(enemy);
        _enemies.Add(enemy);
        enemy.TreeExiting += () => RemoveEnemy(enemy);
    }

    public void RemoveEnemy(ClientEnemy clientEnemy)
    {
        _enemies.Remove(clientEnemy);
    }
    
}
