using Godot;
using KludgeBox.Events;

namespace NeonWarfare;

public readonly record struct BattleWorldSpawnEnemiesRequestEvent(int RequiredEnemiesAmount) : IEvent;
public readonly record struct BattleWorldSpawnEnemyRequest(ServerBattleWorld ServerBattleWorld, Vector2 Position) : IEvent;
public readonly record struct BattleWorldSpawnBossesRequestEvent(int RequiredBossesAmount) : IEvent;
public readonly record struct BattleWorldNewWaveRequestEvent(ServerBattleWorld ServerBattleWorld) : IEvent;
public readonly record struct BattleWorldProcessEvent(ServerBattleWorld ServerBattleWorld, double Delta) : IEvent;