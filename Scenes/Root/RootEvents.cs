using KludgeBox.Events;

namespace NeoVector;

public readonly record struct RootInitEvent : IEvent;
public readonly record struct RootProcessEvent(Root Root, double Delta) : IEvent;

public readonly record struct LogCmdArgsRequest : IEvent;
public readonly record struct NetworkInitRequest : IEvent;

public readonly record struct InitClientRequest : IEvent;