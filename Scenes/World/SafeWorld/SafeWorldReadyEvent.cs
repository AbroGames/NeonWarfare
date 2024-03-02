using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

public readonly struct SafeWorldReadyEvent(SafeWorld safeWorld) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
}