public readonly struct BattleWorldNewWaveEvent(BattleWorld battleWorld, int waveNumber)
{
    public BattleWorld BattleWorld { get; } = battleWorld;
    public int WaveNumber { get; }  = waveNumber;
}