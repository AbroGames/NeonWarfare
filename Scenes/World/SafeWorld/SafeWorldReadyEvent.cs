using KludgeBox.Events;

namespace NeoVector.World;

public readonly struct SafeWorldReadyEvent(SafeWorld safeWorld) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
}