using System;
using Game.Content;
using Godot;
using KludgeBox;

public class BattleWorldWavesService
{
    
    public BattleWorldWavesService()
    {
        Root.Instance.EventBus.Subscribe<BattleWorldProcessEvent>(OnBattleWorldProcessEvent);
    }
    
    public void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldProcessEvent)
    {
        TrySpawnWave(battleWorldProcessEvent.BattleWorld, battleWorldProcessEvent.Delta);
    }
    public void TrySpawnWave(BattleWorld battleWorld, double delta)
    {
        battleWorld.EnemyWave.NextWaveTimer -= delta;
        if (battleWorld.EnemyWave.NextWaveTimer > 0) return;
        
        SpawnWave(battleWorld);
    }
    
    private void SpawnWave(BattleWorld battleWorld)
    {
        EnemyWave enemyWave = battleWorld.EnemyWave;
        
        enemyWave.NextWaveTimer = enemyWave.WaveTimeout;
        enemyWave.WaveNumber++;
		
        for (int i = 0; i < enemyWave.OneWaveEnemyCount + enemyWave.WaveNumber * enemyWave.OneWaveEnemyCountDelta; i++)
        {
            CreateEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(1500, 2500));
        }

        if (enemyWave.WaveNumber % 5 == 0)
        {
            for (int i = 0; i < enemyWave.WaveNumber / 5; i++)
            {
                CreateBossEnemyAroundCharacter(battleWorld, battleWorld.Player, Rand.Double * Mathf.Pi * 2, Rand.Range(2600, 3000));
            }
            Audio2D.PlayUiSound(Sfx.DeepImpact, 1f); // dat bass on boss
            Audio2D.PlayUiSound(Sfx.DeepImpact, 1f); // dat bass on boss again to make it  L O U D E R
        }

        Audio2D.PlayUiSound(Sfx.Bass, 0.8f); // dat bass on start
        Root.Instance.EventBus.Publish(new BattleWorldNewWaveEvent(battleWorld, enemyWave.WaveNumber));
    }
    
    private void CreateEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var enemy = genEnemyAroundCharacter(battleWorld, character, angle, distance);
        battleWorld.AddChild(enemy);
        battleWorld.Enemies.Add(enemy);
    }
	
    private void CreateBossEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var enemy = genEnemyAroundCharacter(battleWorld, character, angle, distance);
        var scale = 1 + 0.1 * battleWorld.EnemyWave.WaveNumber; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Transform = enemy.Transform.ScaledLocal(Vec(scale));  //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.Hp *= 50 * scale; //5 волна = *50, 10 волна = *100, 20 волна = *150 ... и т.д.
        enemy.Damage *= 5 * scale; //5 волна = *5, 10 волна = *10, 20 волна = *15 ... и т.д.
        enemy.MovementSpeed *= scale; //5 волна = 1.5, 10 волна = 2, 20 волна = 3 ... и т.д.
        enemy.BaseXp *= (int) (100 * scale); //5 волна = 150, 10 волна = 200, 20 волна = 300 ... и т.д.
        enemy.IsBoss = true; //
        battleWorld.AddChild(enemy);
        battleWorld.Enemies.Add(enemy);
    }

    private Enemy genEnemyAroundCharacter(BattleWorld battleWorld, Character character, double angle, double distance)
    {
        var targetPositionDelta = Vector2.FromAngle(angle) * distance;
        var targetPosition = character.Position + targetPositionDelta;
			
        var enemy = Root.Instance.PackedScenes.World.Enemy.Instantiate() as Enemy;
        enemy.Position = targetPosition;
        enemy.Rotation = angle - Math.PI / 2;
        enemy.Target = character;
        enemy.MaxHp = 250;
        enemy.Hp = enemy.MaxHp;
        enemy.MovementSpeed = 200; // in pixels/secRegenHpSpeed = 0;
        enemy.Died += () =>
        {
            battleWorld.Enemies.Remove(enemy);
        };
		
        return enemy;
    }
}