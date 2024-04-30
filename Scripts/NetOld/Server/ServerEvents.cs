using KludgeBox.Events;

namespace NeoVector;

public readonly record struct ServerReadyEvent(Server Server) : IEvent;
public readonly record struct ServerProcessEvent(Server Server, double Delta) : IEvent;
public readonly record struct ServerCheckParentIsDeadEvent(Server Server) : IEvent;
