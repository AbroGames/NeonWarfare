using KludgeBox.Events;

namespace NeoVector;

public readonly record struct EnemyStartAttractionEvent(BattleWorld BattleWorld, Enemy Enemy) : IEvent;
public readonly record struct EnemyStopAttractionEvent(BattleWorld BattleWorld, Enemy Enemy) : IEvent;
public readonly record struct EnemyPhysicsProcessEvent(Enemy Enemy, double Delta) : IEvent;
public readonly record struct EnemyProcessEvent(Enemy Enemy, double Delta) : IEvent;
public readonly record struct EnemyReadyEvent(Enemy Enemy) : IEvent;
public readonly record struct EnemyAttackEvent(Enemy Enemy) : IEvent;
public readonly record struct EnemyDeathEvent(Enemy Enemy) : IEvent;
public readonly record struct EnemyAboutToTeleportEvent(Enemy Enemy) : IEvent;