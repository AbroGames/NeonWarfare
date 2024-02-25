using KludgeBox.Events;

public readonly struct TwoColoredBarProcessEvent(TwoColoredBar twoColoredBar, double delta) : IEvent
{
    public TwoColoredBar TwoColoredBar { get; } = twoColoredBar;
    public double Delta { get; } = delta;
}