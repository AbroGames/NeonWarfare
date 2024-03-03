using KludgeBox.Events;

namespace NeoVector;

public readonly struct SafeHudProcessEvent(SafeHud safeHud, double delta) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
    public double Delta { get; }  = delta;
}