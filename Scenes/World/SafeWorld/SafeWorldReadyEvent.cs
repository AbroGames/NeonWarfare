using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

public readonly struct SafeWorldReadyEvent(NeoVector.SafeWorld safeWorld) : IEvent
{
    public NeoVector.SafeWorld SafeWorld { get; } = safeWorld;
}