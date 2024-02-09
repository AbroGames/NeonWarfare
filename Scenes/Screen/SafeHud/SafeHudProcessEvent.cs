public readonly struct SafeHudProcessEvent(SafeHud safeHud, double delta)
{
    public SafeHud SafeHud { get; } = safeHud;
    public double Delta { get; }  = delta;
}