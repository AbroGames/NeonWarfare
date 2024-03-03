using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

public readonly record struct WorldReadyEvent(World World) : IEvent;
public readonly record struct WorldProcessEvent(World World, double Delta) : IEvent;
public readonly record struct WorldPhysicsProcessEvent(World World, double Delta) : IEvent;