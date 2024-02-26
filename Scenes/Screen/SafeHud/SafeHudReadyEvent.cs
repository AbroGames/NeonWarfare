using KludgeBox.Events;

namespace NeoVector;

public readonly struct SafeHudReadyEvent(SafeHud safeHud) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
}