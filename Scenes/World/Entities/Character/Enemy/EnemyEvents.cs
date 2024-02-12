public readonly record struct EnemyStartAttractionEvent(Enemy Enemy);
public readonly record struct EnemyStopAttractionEvent(Enemy Enemy);
public readonly record struct EnemyPhysicsProcessEvent(Enemy Enemy, double Delta);
public readonly record struct EnemyProcessEvent(Enemy Enemy, double Delta);
public readonly record struct EnemyReadyEvent(Enemy Enemy);