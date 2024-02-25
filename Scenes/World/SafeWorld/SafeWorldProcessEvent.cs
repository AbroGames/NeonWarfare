using KludgeBox.Events;

namespace AbroDraft.Scenes.World.SafeWorld;

public readonly struct SafeWorldProcessEvent(SafeWorld safeWorld, double delta) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
    public double Delta { get; }  = delta;
}