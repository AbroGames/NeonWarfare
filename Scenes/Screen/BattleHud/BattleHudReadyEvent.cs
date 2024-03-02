using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly struct BattleHudReadyEvent(BattleHud battleHud) : IEvent
{
    public BattleHud BattleHud { get; } = battleHud;
}