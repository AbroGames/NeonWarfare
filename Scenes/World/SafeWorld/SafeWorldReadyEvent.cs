public readonly struct SafeWorldReadyEvent(SafeWorld safeWorld)
{
    public SafeWorld SafeWorld { get; } = safeWorld;
}