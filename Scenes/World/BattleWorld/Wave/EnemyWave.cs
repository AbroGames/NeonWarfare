namespace NeonWarfare;

public class EnemyWave 
{
	public int OneWaveEnemyCount { get; set; } = 30; 
	public int OneWaveEnemyCountDelta { get; set; } = 2; 
	public int WaveTimeout { get; set; } = 7;

	public int WaveNumber { get; set; } = 0;
	public double NextWaveTimer { get; set; } = 0;
}