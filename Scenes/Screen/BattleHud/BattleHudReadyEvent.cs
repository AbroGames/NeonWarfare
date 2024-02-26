using KludgeBox.Events;

namespace AbroDraft;

public readonly struct BattleHudReadyEvent(BattleHud battleHud) : IEvent
{
    public BattleHud BattleHud { get; } = battleHud;
}