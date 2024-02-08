public readonly struct BattleWorldReadyEvent(BattleWorld battleWorld)
{
    public BattleWorld BattleWorld { get; } = battleWorld;
}