using Godot;
using KludgeBox.Events;

namespace NeonWarfare;

public readonly record struct BattleWorldSpawnEnemiesRequestEvent(int RequiredEnemiesAmount) : IEvent;
public readonly record struct BattleWorldSpawnEnemyRequest(BattleWorld BattleWorld, Vector2 Position) : IEvent;
public readonly record struct BattleWorldSpawnBossesRequestEvent(int RequiredBossesAmount) : IEvent;
public readonly record struct BattleWorldNewWaveRequestEvent(BattleWorld BattleWorld) : IEvent;
public readonly record struct BattleWorldProcessEvent(BattleWorld BattleWorld, double Delta) : IEvent;