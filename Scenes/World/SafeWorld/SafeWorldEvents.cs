using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

public readonly record struct SafeWorldReadyEvent(SafeWorld SafeWorld) : IEvent;
public readonly record struct SafeWorldProcessEvent(SafeWorld SafeWorld, double Delta) : IEvent;