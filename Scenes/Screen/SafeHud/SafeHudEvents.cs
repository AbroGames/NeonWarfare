using KludgeBox.Events;

namespace NeoVector;

public readonly record struct SafeHudReadyEvent(SafeHud SafeHud) : IEvent;
public readonly record struct SafeHudProcessEvent(SafeHud SafeHud, double Delta) : IEvent;
public readonly record struct ToBattleButtonClickEvent(ToBattleButton ToBattleButton) : IEvent;
