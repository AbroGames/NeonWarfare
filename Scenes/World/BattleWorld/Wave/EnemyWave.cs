namespace AbroDraft.Scenes.World.BattleWorld.Wave;

public class EnemyWave 
{
	public int OneWaveEnemyCount { get; set; } = 15; 
	public int OneWaveEnemyCountDelta { get; set; } = 5; 
	public int WaveTimeout { get; set; } = 12;

	public int WaveNumber { get; set; } = 0;
	public double NextWaveTimer { get; set; } = 0;
}