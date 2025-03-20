using System;
using Godot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;
using NeonWarfare.Scripts.Utils.Cooldown;

using static NeonWarfare.Scenes.World.ClientWorld.SC_EnemySpawnPacket;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;

public class EnemyWave
{

    private const int DefaultWaveTime = 60;
    private const int DefaultWaveEnemiesCount = 40;
    
    private EnemySpawner _enemySpawner;
    
    public int WaveNumber = 0;
    public ManualCooldown NextWaveCooldown { get; private set; }

    public EnemyWave(EnemySpawner enemySpawner)
    {
        _enemySpawner = enemySpawner;
        NextWaveSpawn();
    }

    public void Update(double delta)
    {
        NextWaveCooldown.Update(delta);
    }

    private void NextWaveSpawn()
    {
        WaveNumber++;
        SpawnEnemies(EnemyType.Zerg, (int) (DefaultWaveEnemiesCount * Mathf.Pow(1.1, WaveNumber - 1)));
        SpawnEnemies(EnemyType.Shooter, (int) (DefaultWaveEnemiesCount * Mathf.Pow(1.1, WaveNumber - 1) / 8));
        SpawnEnemies(EnemyType.Turtle, (int) (DefaultWaveEnemiesCount * Mathf.Pow(1.1, WaveNumber - 1) / 20));

        double nextWaveTime = DefaultWaveTime / Mathf.Pow(1.1, WaveNumber-1);
        NextWaveCooldown = new(nextWaveTime, false, true, NextWaveSpawn);
    }

    private void SpawnEnemies(EnemyType enemyType, int count)
    {
        _enemySpawner.AddSpawnTask(new EnemySpawnRectTask(enemyType, count, 0, Mathf.DegToRad(360), Vec(0, 0), Vec(8400, 10000)));
    }
 }