using System;
using Godot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

using static NeonWarfare.Scripts.Content.EnemyInfoStorage.EnemyType;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;

public class EnemyWave
{
    private const int DefaultWaveTime = 40;
    
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
        SpawnEnemies(Zerg, WaveNumber*4 + 16); //20,24,28,32,36,40,44,48...
        SpawnEnemies(Shooter, (int) Math.Round(WaveNumber*1.5) + 3); //5,6,7,9,11,12,13,15...
        SpawnEnemies(Turtle, (WaveNumber-1)/2 + 1); //1,1,2,2,3,3,4,4...

        double nextWaveTime = DefaultWaveTime + (WaveNumber-1)*2 ; //40,42,44,46,48,50,52,54...
        NextWaveCooldown = new(nextWaveTime, false, true, NextWaveSpawn);
        
        Network.SendToAll(new ClientBattleWorld.ClientBattleWorld.SC_WaveStartedPacket(WaveNumber));
    }

    private void SpawnEnemies(EnemyInfoStorage.EnemyType enemyType, int count)
    {
        _enemySpawner.AddSpawnTask(new EnemySpawnRingTask(enemyType, count, 0, Mathf.DegToRad(360), Vec(0, 0), 2500, 4200));
    }
 }