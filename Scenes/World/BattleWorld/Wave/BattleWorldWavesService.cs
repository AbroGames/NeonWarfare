using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeonWarfare;

public static class BattleWorldWavesService
{
    
    public static double FadeInTime { get; set; } = 0.5;
    public static double HoldTime { get; set; } = 1;
    public static double FadeOutTime { get; set; } = 0.5;
    
    [EventListener(ListenerSide.Server)]
    public static void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldProcessEvent)
    {
        var (battleWorld, delta) = battleWorldProcessEvent;
        
        battleWorld.EnemyWave.NextWaveTimer -= delta;
        if (battleWorld.EnemyWave.NextWaveTimer > 0) return;
        
        EventBus.Publish(new BattleWorldNewWaveRequestEvent(battleWorld));
    }
    
    [EventListener]
    public static void OnBattleWorldNewWaveRequestEvent(BattleWorldNewWaveRequestEvent battleWorldNewWaveRequestEvent)
    {
        BattleWorld battleWorld = battleWorldNewWaveRequestEvent.BattleWorld;
        EnemyWave enemyWave = battleWorld.EnemyWave;
        
        enemyWave.NextWaveTimer = enemyWave.WaveTimeout;
        enemyWave.WaveNumber++;
        
        EventBus.Publish(new BattleWorldSpawnEnemiesRequestEvent(enemyWave.OneWaveEnemyCount + enemyWave.WaveNumber * enemyWave.OneWaveEnemyCountDelta));

        if (enemyWave.WaveNumber % 2 == 0)
        {
            EventBus.Publish(new BattleWorldSpawnBossesRequestEvent(enemyWave.WaveNumber / 2));
            Audio2D.PlayUiSound(Sfx.DeepImpact, 1f); // dat bass on boss
            // Audio2D.PlayUiSound(Sfx.DeepImpact, 1f); // dat bass on boss again to make it  L O U D E R
        }

        Audio2D.PlayUiSound(Sfx.Bass, 0.8f); // dat bass on start
        battleWorld.BattleHud.PlayNewWaveEffect(enemyWave.WaveNumber);
    }
}