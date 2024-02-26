using KludgeBox.Events;

namespace AbroDraft.World;

public readonly struct SafeWorldReadyEvent(SafeWorld safeWorld) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
}