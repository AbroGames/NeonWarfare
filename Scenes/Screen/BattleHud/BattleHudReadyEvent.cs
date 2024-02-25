using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.BattleHud;

public readonly struct BattleHudReadyEvent(BattleHud battleHud) : IEvent
{
    public BattleHud BattleHud { get; } = battleHud;
}