using KludgeBox.Events;

namespace NeoVector;

public readonly record struct InitRequest() : IEvent;
public readonly record struct LogCmdArgsRequest() : IEvent;


public readonly record struct InitClientRequest() : IEvent;
public readonly record struct InitServerRequest() : IEvent;