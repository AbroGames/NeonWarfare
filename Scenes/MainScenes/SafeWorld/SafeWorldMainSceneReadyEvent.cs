using KludgeBox.Events;

namespace NeoVector;

public readonly struct SafeWorldMainSceneReadyEvent(SafeWorldMainScene safeWorldMainScene) : IEvent
{
    public SafeWorldMainScene SafeWorldMainScene { get; } = safeWorldMainScene;
}