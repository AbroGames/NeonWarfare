using KludgeBox.Events;

namespace NeoVector;

public readonly record struct CreateServerButtonReadyEvent(CreateServerButton CreateServerButton) : IEvent;
public readonly record struct CreateServerButtonClickEvent(CreateServerButton CreateServerButton) : IEvent;
public readonly record struct ConnectToServerButtonClickEvent(ConnectToServerButton CreateServerButton) : IEvent;