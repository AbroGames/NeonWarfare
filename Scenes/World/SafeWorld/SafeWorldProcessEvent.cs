using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

public readonly struct SafeWorldProcessEvent(SafeWorld safeWorld, double delta) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
    public double Delta { get; }  = delta;
}