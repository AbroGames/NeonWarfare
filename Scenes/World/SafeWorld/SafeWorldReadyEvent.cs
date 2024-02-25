using KludgeBox.Events;

namespace AbroDraft.Scenes.World.SafeWorld;

public readonly struct SafeWorldReadyEvent(SafeWorld safeWorld) : IEvent
{
    public SafeWorld SafeWorld { get; } = safeWorld;
}