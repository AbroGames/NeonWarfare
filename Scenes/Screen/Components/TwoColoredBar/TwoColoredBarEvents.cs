using KludgeBox.Events;

public readonly record struct TwoColoredBarReadyEvent(TwoColoredBar TwoColoredBar) : IEvent;
public readonly record struct TwoColoredBarProcessEvent(TwoColoredBar TwoColoredBar, double Delta) : IEvent;