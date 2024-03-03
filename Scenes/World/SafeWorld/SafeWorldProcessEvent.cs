using KludgeBox.Events;

namespace NeoVector;

public readonly struct SafeWorldProcessEvent(NeoVector.SafeWorld safeWorld, double delta) : IEvent
{
    public NeoVector.SafeWorld SafeWorld { get; } = safeWorld;
    public double Delta { get; }  = delta;
}