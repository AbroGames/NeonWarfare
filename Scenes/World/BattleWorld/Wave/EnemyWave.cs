namespace NeoVector;

public class EnemyWave 
{
	public int OneWaveEnemyCount { get; set; } = 30; 
	public int OneWaveEnemyCountDelta { get; set; } = 1; 
	public int WaveTimeout { get; set; } = 12;

	public int WaveNumber { get; set; } = 0;
	public double NextWaveTimer { get; set; } = 0;
}