using KludgeBox.Events;

public readonly struct BattleHudReadyEvent(BattleHud battleHud) : IEvent
{
    public BattleHud BattleHud { get; } = battleHud;
}