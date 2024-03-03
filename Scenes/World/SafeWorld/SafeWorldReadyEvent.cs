using KludgeBox.Events;

namespace NeoVector;

public readonly struct SafeWorldReadyEvent(NeoVector.SafeWorld safeWorld) : IEvent
{
    public NeoVector.SafeWorld SafeWorld { get; } = safeWorld;
}