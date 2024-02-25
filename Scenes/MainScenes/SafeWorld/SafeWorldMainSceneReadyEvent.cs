using KludgeBox.Events;

public readonly struct SafeWorldMainSceneReadyEvent(SafeWorldMainScene safeWorldMainScene) : IEvent
{
    public SafeWorldMainScene SafeWorldMainScene { get; } = safeWorldMainScene;
}