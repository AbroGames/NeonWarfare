﻿using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;

namespace NeonWarfare;

public static class BattleWorldEnemySpawnService
{
    private static int RequiredEnemies = 0;
    private static int RequiredBosses = 0;
    private static int MinEnemiesInGroup = 5;
    private static int MaxEnemiesInGroup = 15;

    private static int NextEnemiesInGroup
    {
        get
        {
            var amount = Mathf.Min(Rand.Range(MinEnemiesInGroup, MaxEnemiesInGroup), RequiredEnemies);
            RequiredEnemies -= amount;
            return amount;
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldPhysicsProcessEvent)
    {
        //TrySpawnWave(battleWorldProcessEvent.BattleWorld, battleWorldProcessEvent.Delta);
        if (RequiredEnemies > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.ClientBattleWorld;
            //CreateEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            //RequiredEnemies--;
            CreateEnemyGroupAroundCharacter(battleWorld, battleWorld.Player, Rand.Range(1000, 1500));
        }

        if (RequiredBosses > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.ClientBattleWorld;
            CreateBossEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            RequiredBosses--;
            
        }
    }
    
    [EventListener]
    public static void OnBattleWorldSpawnEnemiesRequestEvent(BattleWorldSpawnEnemiesRequestEvent battleWorldSpawnEnemiesRequestEvent)
    {
        RequiredEnemies += battleWorldSpawnEnemiesRequestEvent.RequiredEnemiesAmount;
    }
    
    [EventListener]
    public static void OnBattleWorldSpawnBossesRequestEvent(BattleWorldSpawnBossesRequestEvent battleWorldSpawnBossesRequestEvent)
    {
        RequiredBosses += battleWorldSpawnBossesRequestEvent.RequiredBossesAmount;
    }
    
    private static void CreateEnemyAroundCharacter(ClientBattleWorld clientBattleWorld, Character character, double angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(clientBattleWorld, ServerRoot.Instance.PackedScenes.Common.World.Enemy, character, angle, distance);
        AnimateSpawn(enemy, clientBattleWorld);
    }

    [EventListener(ListenerSide.Server)]
    public static void OnBattleWorldSpawnEnemy(BattleWorldSpawnEnemyRequest request)
    {
        var (world, position) = request;
        var enemy = CreateEnemy(world, ServerRoot.Instance.PackedScenes.Common.World.Enemy);
        enemy.Position = position;
        enemy.Target = ServerRoot.Instance.Game.Server.PlayerServerInfo.Values.First().Player;
        enemy.Rotation = Rand.Range(Mathf.Tau);
        AnimateSpawn(enemy, world);

        long nid = ServerRoot.Instance.Game.NetworkEntityManager.AddEntity(enemy);
        Network.SendToAll(new ServerSpawnEnemyPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation, false));
    }
    
    private static void CreateEnemyGroupAroundCharacter(ClientBattleWorld clientBattleWorld, Character character, double radius)
    {
        int amount = NextEnemiesInGroup;
        var spawner = new GroupSpawner();
        spawner.Amount = amount;
        double radiusFactor = ((double)amount - MinEnemiesInGroup) / MaxEnemiesInGroup;
        spawner.Radius = 100 + 500 * radiusFactor;
        spawner.World = clientBattleWorld;

        Vector2 position = Rand.UnitVector * radius;
        spawner.Position = character.Position + position;
        clientBattleWorld.AddChild(spawner);
    }
    
    private static void AnimateSpawn(Enemy enemy, ClientBattleWorld clientBattleWorld)
    {
        if (ServerRoot.Instance is not null) return; //If is server - return
        
        var fx = Fx.CreateSpawnFx();
        fx.Finished += () =>
        {
            clientBattleWorld.AddChild(enemy);
            clientBattleWorld.Enemies.Add(enemy);
            enemy.SkipSmoothing();
        };
        fx.Position = enemy.Position;
        fx.Modulate = enemy.Sprite.Modulate.Darkened(0.33f);
        fx.Scale = enemy.Scale * 0.5;
        clientBattleWorld.AddChild(fx);
    }

    private static int _attractorCounter;
    private static Enemy GenEnemyAroundCharacter(ClientBattleWorld clientBattleWorld, PackedScene template, Character character, double angle, double distance, bool forceAttractor = false)
    {
        var targetPositionDelta = Vector2.FromAngle(angle) * distance;
        var targetPosition = character.Position + targetPositionDelta;

        var enemy = CreateEnemy(clientBattleWorld, template, forceAttractor);
        
        enemy.Position = targetPosition;
        enemy.Rotation = angle - Mathf.Pi / 2;
        enemy.Target = character;

        return enemy;
    }

    private static Enemy CreateEnemy(ClientBattleWorld clientBattleWorld, PackedScene template, bool forceAttractor = false)
    {
        var enemy = template.Instantiate<Enemy>();
        enemy.MaxHp = 250;
        enemy.Hp = enemy.MaxHp;
        enemy.BaseXp *= 1 + clientBattleWorld.EnemyWave.WaveNumber / 10;
        enemy.MovementSpeed = 200; // in pixels/secRegenHpSpeed = 0;
        if (_attractorCounter == 0 || forceAttractor)
        {
            enemy.IsAttractor = true;
            enemy.CollisionPriority = 1000;
            EventBus.Publish(new EnemyStartAttractionEvent(clientBattleWorld, enemy));
        }

        if (!forceAttractor)
        {
            _attractorCounter++;
            _attractorCounter %= 20;
        }
        return enemy;
    }
    
    private static void CreateBossEnemyAroundCharacter(ClientBattleWorld clientBattleWorld, Character character, double angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(clientBattleWorld, ServerRoot.Instance.PackedScenes.Common.World.Boss, character, angle, distance, true);
        var scale = 1 + 0.1 * clientBattleWorld.EnemyWave.WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Scale  = Vec(scale);  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
        enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
        enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.BaseXp += (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
        enemy.IsBoss = true;
        
        AnimateSpawn(enemy, clientBattleWorld);
        long nid = ServerRoot.Instance.Game.NetworkEntityManager.AddEntity(enemy);
        Network.SendToAll(new ServerSpawnEnemyPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation, true));
    }

    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnEnemyPacket(ServerSpawnEnemyPacket serverSpawnEnemyPacket)
    {
        Enemy enemy = (serverSpawnEnemyPacket.IsBoss
            ? ClientRoot.Instance.PackedScenes.Common.World.Boss
            : ClientRoot.Instance.PackedScenes.Common.World.Enemy).Instantiate<Enemy>();
        enemy.IsBoss = serverSpawnEnemyPacket.IsBoss;
        enemy.Position = Vec(serverSpawnEnemyPacket.X, serverSpawnEnemyPacket.Y);
        enemy.Rotation = serverSpawnEnemyPacket.Dir;
        
        ClientRoot.Instance.Game.World.AddChild(enemy);
        ClientRoot.Instance.Game.NetworkEntityManager.AddEntity(enemy, serverSpawnEnemyPacket.Nid);
    }
}