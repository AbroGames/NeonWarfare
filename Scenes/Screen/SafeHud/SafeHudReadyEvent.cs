using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly struct SafeHudReadyEvent(SafeHud safeHud) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
}