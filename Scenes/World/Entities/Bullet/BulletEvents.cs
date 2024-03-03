using KludgeBox.Events;

namespace NeoVector;

public readonly record struct BulletReadyEvent(Bullet Bullet) : IEvent;
public readonly record struct BulletHitEvent(Bullet Bullet, Character Target) : IEvent;
public readonly record struct BulletProcessEvent(Bullet Bullet, double Delta) : IEvent;