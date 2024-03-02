using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly struct SafeWorldMainSceneReadyEvent(SafeWorldMainScene safeWorldMainScene) : IEvent
{
    public SafeWorldMainScene SafeWorldMainScene { get; } = safeWorldMainScene;
}