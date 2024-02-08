public readonly struct TwoColoredBarProcessEvent(TwoColoredBar twoColoredBar, double delta)
{
    public TwoColoredBar TwoColoredBar { get; } = twoColoredBar;
    public double Delta { get; } = delta;
}