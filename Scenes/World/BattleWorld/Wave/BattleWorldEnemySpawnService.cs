using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Utils;

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
            var battleWorld = battleWorldPhysicsProcessEvent.ServerBattleWorld;
            //CreateEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            //RequiredEnemies--;
            //CreateEnemyGroupAroundCharacter(battleWorld, battleWorld.Player, Rand.Range(1000, 1500));
        }

        if (RequiredBosses > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.ServerBattleWorld;
            //CreateBossEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Float * Mathf.Pi * 2, Rand.Range(1500, 2500));
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
    
    private static void CreateEnemyAroundCharacter(ServerBattleWorld serverBattleWorld, Character character, float angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(serverBattleWorld, ServerRoot.Instance.PackedScenes.Enemy, character, angle, distance);
    }

    [EventListener(ListenerSide.Server)]
    public static void OnBattleWorldSpawnEnemy(BattleWorldSpawnEnemyRequest request)
    {
        var (world, position) = request;
        var enemy = CreateEnemy(world, ServerRoot.Instance.PackedScenes.Enemy);
        enemy.Position = position;
        enemy.Target = ServerRoot.Instance.Game.PlayerProfiles.First().Player;
        enemy.Rotation = Rand.Range(Mathf.Tau);

        long nid = ServerRoot.Instance.Game.World.NetworkEntityManager.AddEntity(enemy);
        Network.SendToAll(new ServerSpawnEnemyPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation, false));
    }
    
    private static void CreateEnemyGroupAroundCharacter(ServerBattleWorld serverBattleWorld, Character character, double radius)
    {
        int amount = NextEnemiesInGroup;
        var spawner = new GroupSpawner();
        spawner.Amount = amount;
        double radiusFactor = ((double)amount - MinEnemiesInGroup) / MaxEnemiesInGroup;
        spawner.Radius = 100 + 500 * (float) radiusFactor;
        spawner.World = serverBattleWorld;

        Vector2 position = Rand.UnitVector * (float) radius;
        spawner.Position = character.Position + position;
        serverBattleWorld.AddChild(spawner);
    }
    
    private static void AnimateSpawn(Enemy enemy, ClientBattleWorld clientBattleWorld)
    {
        if (CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) return; //If is server - return
        
        var fx = Fx.CreateSpawnFx();
        fx.Finished += () =>
        {
            clientBattleWorld.AddChild(enemy);
            clientBattleWorld.Enemies.Add(enemy);
            enemy.SkipSmoothing();
        };
        fx.Position = enemy.Position;
        fx.Modulate = enemy.Sprite.Modulate.Darkened(0.33f);
        fx.Scale = enemy.Scale * 0.5f;
        clientBattleWorld.AddChild(fx);
    }

    private static int _attractorCounter;
    private static Enemy GenEnemyAroundCharacter(ServerBattleWorld serverBattleWorld, PackedScene template, Character character, float angle, double distance, bool forceAttractor = false)
    {
        var targetPositionDelta = Vector2.FromAngle(angle) * (float) distance;
        var targetPosition = character.Position + targetPositionDelta;

        var enemy = CreateEnemy(serverBattleWorld, template, forceAttractor);
        
        enemy.Position = targetPosition;
        enemy.Rotation = angle - Mathf.Pi / 2;
        enemy.Target = character;

        return enemy;
    }

    private static Enemy CreateEnemy(ServerBattleWorld serverBattleWorld, PackedScene template, bool forceAttractor = false)
    {
        var enemy = template.Instantiate<Enemy>();
        enemy.MaxHp = 250;
        enemy.Hp = enemy.MaxHp;
        enemy.BaseXp *= 1 + serverBattleWorld.EnemyWave.WaveNumber / 10;
        enemy.MovementSpeed = 200; // in pixels/secRegenHpSpeed = 0;
        if (_attractorCounter == 0 || forceAttractor)
        {
            enemy.IsAttractor = true;
            enemy.CollisionPriority = 1000;
            EventBus.Publish(new EnemyStartAttractionEvent(serverBattleWorld, enemy));
        }

        if (!forceAttractor)
        {
            _attractorCounter++;
            _attractorCounter %= 20;
        }
        return enemy;
    }
    
    private static void CreateBossEnemyAroundCharacter(ServerBattleWorld serverBattleWorld, Character character, float angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(serverBattleWorld, ServerRoot.Instance.PackedScenes.Boss, character, angle, distance, true);
        var scale = 1 + 0.1 * serverBattleWorld.EnemyWave.WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Scale  = Vec((float) scale);  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
        enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
        enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.BaseXp += (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
        enemy.IsBoss = true;
        
        long nid = ServerRoot.Instance.Game.World.NetworkEntityManager.AddEntity(enemy);
        Network.SendToAll(new ServerSpawnEnemyPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation, true));
    }

    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnEnemyPacket(ServerSpawnEnemyPacket serverSpawnEnemyPacket)
    {
        Enemy enemy = (serverSpawnEnemyPacket.IsBoss
            ? ClientRoot.Instance.PackedScenes.Boss
            : ClientRoot.Instance.PackedScenes.Enemy).Instantiate<Enemy>();
        enemy.IsBoss = serverSpawnEnemyPacket.IsBoss;
        enemy.Position = Vec((float) serverSpawnEnemyPacket.X, (float) serverSpawnEnemyPacket.Y);
        enemy.Rotation = (float) serverSpawnEnemyPacket.Dir;
        
        ClientRoot.Instance.Game.World.AddChild(enemy);
        ClientRoot.Instance.Game.World.NetworkEntityManager.AddEntity(enemy, serverSpawnEnemyPacket.Nid);
    }
}