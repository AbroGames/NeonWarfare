﻿using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class BattleWorldEnemySpawnService
{
    private int RequiredEnemies = 0;
    private int RequiredBosses = 0;
    private int MinEnemiesInGroup = 5;
    private int MaxEnemiesInGroup = 15;

    private int NextEnemiesInGroup
    {
        get
        {
            var amount = Mathf.Min(Rand.Range(MinEnemiesInGroup, MaxEnemiesInGroup), RequiredEnemies);
            RequiredEnemies -= amount;
            return amount;
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldPhysicsProcessEvent)
    {
        //TrySpawnWave(battleWorldProcessEvent.BattleWorld, battleWorldProcessEvent.Delta);
        if (RequiredEnemies > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.BattleWorld;
            //CreateEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            //RequiredEnemies--;
            CreateEnemyGroupAroundCharacter(battleWorld, battleWorld.Player, Rand.Range(1000, 1500));
        }

        if (RequiredBosses > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.BattleWorld;
            CreateBossEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            RequiredBosses--;
            
        }
    }
    
    [EventListener]
    public void OnBattleWorldSpawnEnemiesRequestEvent(BattleWorldSpawnEnemiesRequestEvent battleWorldSpawnEnemiesRequestEvent)
    {
        RequiredEnemies += battleWorldSpawnEnemiesRequestEvent.RequiredEnemiesAmount;
    }
    
    [EventListener]
    public void OnBattleWorldSpawnBossesRequestEvent(BattleWorldSpawnBossesRequestEvent battleWorldSpawnBossesRequestEvent)
    {
        RequiredBosses += battleWorldSpawnBossesRequestEvent.RequiredBossesAmount;
    }
    
    private void CreateEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(battleWorld, Root.Instance.PackedScenes.World.Enemy, character, angle, distance);
        AnimateSpawn(enemy, battleWorld);
    }

    [EventListener(ListenerSide.Server)]
    public void OnBattleWorldSpawnEnemy(BattleWorldSpawnEnemyRequest request)
    {
        var (world, position) = request;
        var enemy = CreateEnemy(world, Root.Instance.PackedScenes.World.Enemy);
        enemy.Position = position;
        enemy.Target = Root.Instance.Server.PlayerServerInfo.Values.First().Player;
        enemy.Rotation = Rand.Range(Mathf.Tau);
        AnimateSpawn(enemy, world);

        long nid = Root.Instance.NetworkEntityManager.AddEntity(enemy);
        Network.SendPacketToClients(new ServerSpawnEnemyPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation, false));
    }
    
    private void CreateEnemyGroupAroundCharacter(BattleWorld battleWorld, Character character, double radius)
    {
        int amount = NextEnemiesInGroup;
        var spawner = new GroupSpawner();
        spawner.Amount = amount;
        double radiusFactor = ((double)amount - MinEnemiesInGroup) / MaxEnemiesInGroup;
        spawner.Radius = 100 + 500 * radiusFactor;
        spawner.World = battleWorld;

        Vector2 position = Rand.UnitVector * radius;
        spawner.Position = character.Position + position;
        battleWorld.AddChild(spawner);
    }
    
    private void AnimateSpawn(Enemy enemy, BattleWorld battleWorld)
    {
        var fx = Fx.CreateSpawnFx();
        fx.Finished += () =>
        {
            battleWorld.AddChild(enemy);
            battleWorld.Enemies.Add(enemy);
            enemy.SkipSmoothing();
        };
        fx.Position = enemy.Position;
        fx.Modulate = enemy.Sprite.Modulate.Darkened(0.33f);
        fx.Scale = enemy.Scale * 0.5;
        battleWorld.AddChild(fx);
    }

    private int _attractorCounter;
    private Enemy GenEnemyAroundCharacter(BattleWorld battleWorld, PackedScene template, Character character, double angle, double distance, bool forceAttractor = false)
    {
        var targetPositionDelta = Vector2.FromAngle(angle) * distance;
        var targetPosition = character.Position + targetPositionDelta;

        var enemy = CreateEnemy(battleWorld, template, forceAttractor);
        
        enemy.Position = targetPosition;
        enemy.Rotation = angle - Mathf.Pi / 2;
        enemy.Target = character;

        return enemy;
    }

    private Enemy CreateEnemy(BattleWorld battleWorld, PackedScene template, bool forceAttractor = false)
    {
        var enemy = template.Instantiate() as Enemy;
        enemy.MaxHp = 250;
        enemy.Hp = enemy.MaxHp;
        enemy.BaseXp *= 1 + battleWorld.EnemyWave.WaveNumber / 10;
        enemy.MovementSpeed = 200; // in pixels/secRegenHpSpeed = 0;
        if (_attractorCounter == 0 || forceAttractor)
        {
            enemy.IsAttractor = true;
            enemy.CollisionPriority = 1000;
            EventBus.Publish(new EnemyStartAttractionEvent(battleWorld, enemy));
        }

        if (!forceAttractor)
        {
            _attractorCounter++;
            _attractorCounter %= 20;
        }
        return enemy;
    }
    
    private void CreateBossEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(battleWorld, Root.Instance.PackedScenes.World.Boss, character, angle, distance, true);
        var scale = 1 + 0.1 * battleWorld.EnemyWave.WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Scale  = Vec(scale);  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
        enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
        enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.BaseXp += (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
        enemy.IsBoss = true;
        
        AnimateSpawn(enemy, battleWorld);
        long nid = Root.Instance.NetworkEntityManager.AddEntity(enemy);
        Network.SendPacketToClients(new ServerSpawnEnemyPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation, true));
    }

    [EventListener]
    public void OnServerSpawnEnemyPacket(ServerSpawnEnemyPacket serverSpawnEnemyPacket)
    {
        Enemy enemy = (serverSpawnEnemyPacket.IsBoss
            ? Root.Instance.PackedScenes.World.Boss
            : Root.Instance.PackedScenes.World.Enemy).Instantiate<Enemy>();
        enemy.IsBoss = serverSpawnEnemyPacket.IsBoss;
        enemy.Position = Vec(serverSpawnEnemyPacket.X, serverSpawnEnemyPacket.Y);
        enemy.Rotation = serverSpawnEnemyPacket.Dir;
        
        Root.Instance.CurrentWorld.AddChild(enemy);
        Root.Instance.NetworkEntityManager.AddEntity(enemy, serverSpawnEnemyPacket.Nid);
    }
}