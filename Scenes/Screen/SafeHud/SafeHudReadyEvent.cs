using KludgeBox.Events;

public readonly struct SafeHudReadyEvent(SafeHud safeHud) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
}