using KludgeBox.Events;

namespace NeonWarfare;

public readonly record struct EnemyStartAttractionEvent(ServerBattleWorld serverBattleWorld, Enemy Enemy) : IEvent;
public readonly record struct EnemyStopAttractionEvent(ServerBattleWorld serverBattleWorld, Enemy Enemy) : IEvent;
public readonly record struct EnemyPhysicsProcessEvent(Enemy Enemy, double Delta) : IEvent;
public readonly record struct EnemyProcessEvent(Enemy Enemy, double Delta) : IEvent;
public readonly record struct EnemyReadyEvent(Enemy Enemy) : IEvent;
public readonly record struct EnemyAttackEvent(Enemy Enemy) : IEvent;
public readonly record struct EnemyDeathEvent(Enemy Enemy) : IEvent;
public readonly record struct EnemyAboutToTeleportEvent(Enemy Enemy) : IEvent;