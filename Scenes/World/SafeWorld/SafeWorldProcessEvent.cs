using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

public readonly struct SafeWorldProcessEvent(NeoVector.SafeWorld safeWorld, double delta) : IEvent
{
    public NeoVector.SafeWorld SafeWorld { get; } = safeWorld;
    public double Delta { get; }  = delta;
}