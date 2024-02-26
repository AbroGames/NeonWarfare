using KludgeBox.Events;

namespace AbroDraft;

public readonly struct SafeHudProcessEvent(SafeHud safeHud, double delta) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
    public double Delta { get; }  = delta;
}