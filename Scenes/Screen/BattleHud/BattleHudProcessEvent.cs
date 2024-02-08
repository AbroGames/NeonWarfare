public readonly struct BattleHudProcessEvent(BattleHud battleHud, double delta)
{
    public BattleHud BattleHud { get; } = battleHud;
    public double Delta { get; }  = delta;
}