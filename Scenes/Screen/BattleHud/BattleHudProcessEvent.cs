public readonly struct BattleHudProcessEvent(BattleHud BattleHud, double delta)
{
    public BattleHud BattleHud { get; } = BattleHud;
    public double Delta { get; }  = delta;
}