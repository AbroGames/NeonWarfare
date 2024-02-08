public readonly struct BattleWorldProcessEvent(BattleWorld battleWorld, double delta)
{
    public BattleWorld BattleWorld { get; } = battleWorld;
    public double Delta { get; }  = delta;
}