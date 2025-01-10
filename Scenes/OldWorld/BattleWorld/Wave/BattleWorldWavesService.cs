using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeonWarfare;

public static class BattleWorldWavesService
{
    
    public static double FadeInTime { get; set; } = 0.5;
    public static double HoldTime { get; set; } = 1;
    public static double FadeOutTime { get; set; } = 0.5;
    
    //[EventListener(ListenerSide.Server)]
    public static void OnBattleWorldProcessEvent(BattleWorldProcessEvent battleWorldProcessEvent)
    {
        var (battleWorld, delta) = battleWorldProcessEvent;
        
        //battleWorld.EnemyWave.NextWaveTimer -= delta;
        //if (battleWorld.EnemyWave.NextWaveTimer > 0) return;
        
        EventBus.Publish(new BattleWorldNewWaveRequestEvent(battleWorld));
    }
    
    //[EventListener(ListenerSide.Server)]
    public static void OnBattleWorldNewWaveRequestEvent(BattleWorldNewWaveRequestEvent battleWorldNewWaveRequestEvent)
    {
        ServerBattleWorld serverBattleWorld = battleWorldNewWaveRequestEvent.ServerBattleWorld;
        EnemyWave enemyWave = null; //serverBattleWorld.EnemyWave;
        
        enemyWave.NextWaveTimer = enemyWave.WaveTimeout;
        enemyWave.WaveNumber++;
        
        EventBus.Publish(new BattleWorldSpawnEnemiesRequestEvent(enemyWave.OneWaveEnemyCount + enemyWave.WaveNumber * enemyWave.OneWaveEnemyCountDelta));

        if (enemyWave.WaveNumber % 2 == 0)
        {
            EventBus.Publish(new BattleWorldSpawnBossesRequestEvent(enemyWave.WaveNumber / 2));
        }
    }
}