using KludgeBox.Events;

namespace NeoVector;

public readonly record struct RootInitEvent() : IEvent;
public readonly record struct LogCmdArgsRequest() : IEvent;

public readonly record struct InitClientRequest() : IEvent;