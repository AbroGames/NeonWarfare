public readonly struct SafeHudReadyEvent(SafeHud safeHud)
{
    public SafeHud SafeHud { get; } = safeHud;
}