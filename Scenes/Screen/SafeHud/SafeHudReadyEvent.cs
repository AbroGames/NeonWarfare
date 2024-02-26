using KludgeBox.Events;

namespace AbroDraft;

public readonly struct SafeHudReadyEvent(SafeHud safeHud) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
}