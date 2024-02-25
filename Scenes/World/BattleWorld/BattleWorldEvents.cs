using KludgeBox.Events;

namespace AbroDraft.Scenes.World.BattleWorld;

public readonly record struct BattleWorldSpawnEnemiesRequestEvent(int RequiredEnemiesAmount) : IEvent;
public readonly record struct BattleWorldSpawnBossesRequestEvent(int RequiredBossesAmount) : IEvent;
public readonly record struct BattleWorldNewWaveEvent(BattleWorld BattleWorld, int WaveNumber) : IEvent;
public readonly record struct BattleWorldProcessEvent(BattleWorld BattleWorld, double Delta) : IEvent;
public readonly record struct BattleWorldDeferredProcessEvent(BattleWorld BattleWorld, double Delta) : IEvent;
public readonly record struct BattleWorldPhysicsProcessEvent(BattleWorld BattleWorld, double Delta) : IEvent;
public readonly record struct BattleWorldDeferredPhysicsProcessEvent(BattleWorld BattleWorld, double Delta) : IEvent;
public readonly record struct BattleWorldReadyEvent(BattleWorld BattleWorld) : IEvent;