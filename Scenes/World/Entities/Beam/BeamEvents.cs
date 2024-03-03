using KludgeBox.Events;

namespace NeoVector;

public readonly record struct BeamSpawnedEvent(Beam Beam) : IEvent;
public readonly record struct SolarBeamSpawnedEvent(SolarBeam Beam) : IEvent;

public readonly record struct BeamPhysicsProcessEvent(Beam Beam, double Delta) : IEvent;
public readonly record struct SolarBeamPhysicsProcessEvent(SolarBeam Beam, double Delta) : IEvent;

public readonly record struct BeamDestroyedEvent(Beam Beam) : IEvent;
public readonly record struct SolarBeamDestroyedEvent(SolarBeam Beam) : IEvent;

public readonly record struct BeamDamageEvent(Beam Beam, double Damage) : IEvent;
public readonly record struct SolarBeamDamageEvent(SolarBeam Beam, double Damage) : IEvent;