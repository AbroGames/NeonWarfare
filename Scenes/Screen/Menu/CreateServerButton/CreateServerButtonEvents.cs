using KludgeBox.Events;

namespace KludgeBox.Events.Global;

public readonly record struct CreateServerButtonReadyEvent(CreateServerButton CreateServerButton) : IEvent;
public readonly record struct CreateServerButtonClickEvent(CreateServerButton CreateServerButton) : IEvent;