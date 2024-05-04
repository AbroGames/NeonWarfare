using KludgeBox.Events;

namespace NeonWarfare;

public readonly record struct ShutDownEvent(int ExitCode = 0) : IEvent;