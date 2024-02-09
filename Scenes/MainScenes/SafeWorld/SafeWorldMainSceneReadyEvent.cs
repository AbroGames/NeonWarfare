public readonly struct SafeWorldMainSceneReadyEvent(SafeWorldMainScene safeWorldMainScene)
{
    public SafeWorldMainScene SafeWorldMainScene { get; } = safeWorldMainScene;
}