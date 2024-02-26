using KludgeBox.Events;

namespace NeoVector;

public readonly struct BattleHudReadyEvent(BattleHud battleHud) : IEvent
{
    public BattleHud BattleHud { get; } = battleHud;
}