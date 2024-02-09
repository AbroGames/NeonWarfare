public readonly struct SafeWorldProcessEvent(SafeWorld safeWorld, double delta)
{
    public SafeWorld SafeWorld { get; } = safeWorld;
    public double Delta { get; }  = delta;
}