using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

public class EnemySpawner
{
    private const int MaxSpawnInSecond = 25;
    private const int MaxSpawnInOneUpdate = 5;

    public IReadOnlyList<EnemySpawnTask> SpawnTasks => _spawnTasks.Select(taskCounter => taskCounter.EnemySpawnTask).ToList();
    
    private ServerBattleWorld _serverBattleWorld;
    private AutoCooldown _nextSpawnCooldown = new(1.0/MaxSpawnInSecond);
    private int _spawnInCurrentUpdate = 0;
    
    private List<EnemySpawnTaskCounter> _spawnTasks = new();

    public EnemySpawner(ServerBattleWorld serverBattleWorld)
    {
        _serverBattleWorld = serverBattleWorld;
        _nextSpawnCooldown.ActionWhenReady += TrySpawnNextEnemy;
    }
    
    public void Update(double delta)
    {
        _spawnInCurrentUpdate = 0;
        _nextSpawnCooldown.Update(delta);
    }

    public void AddSpawnTask(EnemySpawnTask enemySpawnTask)
    {
        _spawnTasks.Add(new EnemySpawnTaskCounter(enemySpawnTask));
    }

    public void ClearSpawnTasks()
    {
        _spawnTasks.Clear();
    }
    
    private void TrySpawnNextEnemy()
    {
        if (_spawnInCurrentUpdate >= MaxSpawnInOneUpdate) return;
        if (_spawnTasks.Count == 0) return;
        _spawnInCurrentUpdate++;

        SpawnEnemy(_spawnTasks.First().EnemySpawnTask);
        
        _spawnTasks.First().SpawnedCount++;
        if (_spawnTasks.First().SpawnedCount >= _spawnTasks.First().EnemySpawnTask.Count)
        {
            _spawnTasks.RemoveAt(0);
        }
    }

    private void SpawnEnemy(EnemySpawnTask spawnTask)
    {
        EnemyInfoStorage.EnemyType enemyType = spawnTask.EnemyType;
        Vector2 position = spawnTask.GenSpawnPoint();
        float rotation = spawnTask.GenRotation();
        _serverBattleWorld.SpawnEnemy(enemyType, position, rotation);
    }

    private record EnemySpawnTaskCounter
    {
        public EnemySpawnTask EnemySpawnTask { get; set; }
        public int SpawnedCount { get; set; } = 0;

        public EnemySpawnTaskCounter(EnemySpawnTask enemySpawnTask)
        {
            EnemySpawnTask = enemySpawnTask;
        }
    }
}