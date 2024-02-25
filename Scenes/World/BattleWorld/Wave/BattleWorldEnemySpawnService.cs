using AbroDraft.Scenes.World.Entities.Character;
using AbroDraft.Scenes.World.Entities.Character.Enemy;
using AbroDraft.Scripts.Content;
using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft.Scenes.World.BattleWorld.Wave;

[GameService]
public class BattleWorldEnemySpawnService
{
    private int RequiredEnemies = 0;
    private int RequiredBosses = 0;
    
    [GameEventListener]
    public void OnBattleWorldPhysicsProcessEvent(BattleWorldPhysicsProcessEvent battleWorldPhysicsProcessEvent)
    {
        //TrySpawnWave(battleWorldProcessEvent.BattleWorld, battleWorldProcessEvent.Delta);
        if (RequiredEnemies > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.BattleWorld;
            CreateEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            RequiredEnemies--;
        }

        if (RequiredBosses > 0)
        {
            var battleWorld = battleWorldPhysicsProcessEvent.BattleWorld;
            CreateBossEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
            RequiredBosses--;
        }
    }
    
    [GameEventListener]
    public void OnBattleWorldSpawnEnemiesRequestEvent(BattleWorldSpawnEnemiesRequestEvent battleWorldSpawnEnemiesRequestEvent)
    {
        RequiredEnemies += battleWorldSpawnEnemiesRequestEvent.RequiredEnemiesAmount;
    }
    
    [GameEventListener]
    public void OnBattleWorldSpawnBossesRequestEvent(BattleWorldSpawnBossesRequestEvent battleWorldSpawnBossesRequestEvent)
    {
        RequiredBosses += battleWorldSpawnBossesRequestEvent.RequiredBossesAmount;
    }
    
    private void CreateEnemyAroundCharacter(BattleWorld battleWorld, Entities.Character.Character character, double angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(battleWorld, Root.Root.Instance.PackedScenes.World.Enemy, character, angle, distance);
        AnimateSpawn(enemy, battleWorld);
    }

    private void AnimateSpawn(Entities.Character.Enemy.Enemy enemy, BattleWorld battleWorld)
    {
        var fx = Fx.CreateSpawnFx();
        fx.Finished += () =>
        {
            battleWorld.AddChild(enemy);
            battleWorld.Enemies.Add(enemy);
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
			
        var enemy = template.Instantiate() as Entities.Character.Enemy.Enemy;
        enemy.Position = targetPosition;
        enemy.Rotation = angle - Mathf.Pi / 2;
        enemy.Target = character;
        enemy.MaxHp = 250;
        enemy.Hp = enemy.MaxHp;
        enemy.BaseXp *= 1 + battleWorld.EnemyWave.WaveNumber / 10;
        enemy.MovementSpeed = 200; // in pixels/secRegenHpSpeed = 0;
        if (_attractorCounter == 0 || forceAttractor)
        {
            enemy.IsAttractor = true;
            enemy.CollisionPriority = 1000;
            EventBus.Publish(new EnemyStartAttractionEvent(enemy));
        }
        enemy.Died += () =>
        {
            battleWorld.Enemies.Remove(enemy);
            if (enemy.IsAttractor)
            {
                EventBus.Publish(new EnemyStopAttractionEvent(enemy));
            }
        };

        if (!forceAttractor)
        {
            _attractorCounter++;
            _attractorCounter %= 20;
        }
        return enemy;
    }
    
    private void CreateBossEnemyAroundCharacter(BattleWorld battleWorld, Entities.Character.Character character, double angle, double distance)
    {
        var enemy = GenEnemyAroundCharacter(battleWorld, Root.Root.Instance.PackedScenes.World.Boss, character, angle, distance, true);
        var scale = 1 + 0.1 * battleWorld.EnemyWave.WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Scale  = Vec(scale);  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
        enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
        enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.BaseXp += (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
        enemy.IsBoss = true;
        
        AnimateSpawn(enemy, battleWorld);
    }
}