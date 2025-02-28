using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    public IReadOnlyList<ClientEnemy> Enemies => _enemies;
    private List<ClientEnemy> _enemies = new(); 
    
    public void OnEnemySpawnPacket(SC_EnemySpawnPacket enemySpawnPacket)
    {
        ClientEnemy enemy = CreateNetworkEntity<ClientEnemy>(
            ClientRoot.Instance.PackedScenes.Enemy, enemySpawnPacket.Nid);
        enemy.AddChild(new NetworkInertiaComponent());
        enemy.TreeExiting += () => RemoveEnemy(enemy);
        
        AddChild(enemy);
        _enemies.Add(enemy);
        enemy.OnSpawnPacket(enemySpawnPacket.Position, enemySpawnPacket.Dir, enemySpawnPacket.Color);
    }

    public void RemoveEnemy(ClientEnemy clientEnemy)
    {
        _enemies.Remove(clientEnemy);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnEnemySpawnPacketListener(SC_EnemySpawnPacket enemySpawnPacket)
    {
        ClientRoot.Instance.Game.World.OnEnemySpawnPacket(enemySpawnPacket);
    }
}
