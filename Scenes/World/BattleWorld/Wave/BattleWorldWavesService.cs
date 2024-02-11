using System;
using Game.Content;
using Godot;
using KludgeBox;

[GameService]
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
		
        
        Root.Instance.EventBus.Publish(new BattleWorldSpawnEnemiesRequestEvent(enemyWave.OneWaveEnemyCount + enemyWave.WaveNumber * enemyWave.OneWaveEnemyCountDelta));

        if (enemyWave.WaveNumber % 5 == 0)
        {
            Root.Instance.EventBus.Publish(new BattleWorldSpawnBossesRequestEvent(enemyWave.WaveNumber / 5));
            Audio2D.PlayUiSound(Sfx.DeepImpact, 1f); // dat bass on boss
            Audio2D.PlayUiSound(Sfx.DeepImpact, 1f); // dat bass on boss again to make it  L O U D E R
        }

        Audio2D.PlayUiSound(Sfx.Bass, 0.8f); // dat bass on start
        Root.Instance.EventBus.Publish(new BattleWorldNewWaveEvent(battleWorld, enemyWave.WaveNumber));
    }
}