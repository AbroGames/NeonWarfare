using Godot;
using KludgeBox.Events;

namespace NeonWarfare;

public readonly record struct BattleWorldSpawnEnemiesRequestEvent(int RequiredEnemiesAmount) : IEvent;
public readonly record struct BattleWorldSpawnEnemyRequest(ClientBattleWorld ClientBattleWorld, Vector2 Position) : IEvent;
public readonly record struct BattleWorldSpawnBossesRequestEvent(int RequiredBossesAmount) : IEvent;
public readonly record struct BattleWorldNewWaveRequestEvent(ClientBattleWorld ClientBattleWorld) : IEvent;
public readonly record struct BattleWorldProcessEvent(ClientBattleWorld ClientBattleWorld, double Delta) : IEvent;