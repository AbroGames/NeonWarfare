using KludgeBox.Events;

public readonly struct SafeWorldReadyEvent(SafeWorld safeWorld) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
}