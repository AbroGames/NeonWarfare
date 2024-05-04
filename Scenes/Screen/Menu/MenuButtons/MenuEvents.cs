using Godot;
using KludgeBox.Events;

namespace NeoVector;

public readonly record struct ShutDownEvent(int ExitCode = 0) : IEvent;