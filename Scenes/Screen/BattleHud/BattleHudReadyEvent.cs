public readonly struct BattleHudReadyEvent(BattleHud battleHud)
{
    public BattleHud BattleHud { get; } = battleHud;
}