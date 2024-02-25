using KludgeBox.Events;

namespace AbroDraft.Scenes.MainScenes.SafeWorld;

public readonly struct SafeWorldMainSceneReadyEvent(SafeWorldMainScene safeWorldMainScene) : IEvent
{
    public SafeWorldMainScene SafeWorldMainScene { get; } = safeWorldMainScene;
}