public readonly record struct BattleWorldSpawnEnemiesRequestEvent(int RequiredEnemiesAmount);
public readonly record struct BattleWorldSpawnBossesRequestEvent(int RequiredBossesAmount);
public readonly record struct BattleWorldNewWaveEvent(BattleWorld BattleWorld, int WaveNumber);
public readonly record struct BattleWorldProcessEvent(BattleWorld BattleWorld, double Delta);
public readonly record struct BattleWorldDeferredProcessEvent(BattleWorld BattleWorld, double Delta);
public readonly record struct BattleWorldPhysicsProcessEvent(BattleWorld BattleWorld, double Delta);
public readonly record struct BattleWorldDeferredPhysicsProcessEvent(BattleWorld BattleWorld, double Delta);
public readonly record struct BattleWorldReadyEvent(BattleWorld BattleWorld);