using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

using static NeonWarfare.Scenes.World.ClientWorld.SC_EnemySpawnPacket;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld
{

    public IReadOnlyList<ServerEnemy> Enemies => _enemies;
    private List<ServerEnemy> _enemies = new();

    public ServerEnemy SpawnEnemy(EnemyType type, Vector2 position, float rotation)
    {
        ServerEnemy enemy = CreateNetworkEntity<ServerEnemy>(EnemyScenesMap[type].ServerScene.Invoke());
        enemy.InitComponents();
        enemy.InitOnSpawn(position, rotation);
        
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
