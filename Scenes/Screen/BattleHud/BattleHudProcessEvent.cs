using KludgeBox.Events;

namespace AbroDraft;

public readonly record struct BattleHudProcessEvent(BattleHud BattleHud, double Delta) : IEvent;
public readonly record struct BattleHudPhysicsProcessEvent(BattleHud BattleHud, double Delta) : IEvent;