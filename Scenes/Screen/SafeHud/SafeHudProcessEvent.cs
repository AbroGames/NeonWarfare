using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly struct SafeHudProcessEvent(SafeHud safeHud, double delta) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
    public double Delta { get; }  = delta;
}