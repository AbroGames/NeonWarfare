using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.SafeHud;

public readonly struct SafeHudProcessEvent(SafeHud safeHud, double delta) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
    public double Delta { get; }  = delta;
}