using KludgeBox.Events;

public readonly struct TwoColoredBarReadyEvent(TwoColoredBar twoColoredBar) : IEvent
{
    public TwoColoredBar TwoColoredBar { get; } = twoColoredBar;
}