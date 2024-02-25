using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.Components.TwoColoredBar;

public readonly record struct TwoColoredBarReadyEvent(TwoColoredBar TwoColoredBar) : IEvent;
public readonly record struct TwoColoredBarProcessEvent(TwoColoredBar TwoColoredBar, double Delta) : IEvent;