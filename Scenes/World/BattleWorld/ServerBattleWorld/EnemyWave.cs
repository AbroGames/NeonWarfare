using System;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
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
        if (WaveNumber == 0)
        {
            WaveNumber = 1;
        }
        else
        {
            WaveNumber += ServerRoot.Instance.Game.GameSettings.WaveInc;
        }

        string type = ServerRoot.Instance.Game.GameSettings.WaveType;
        switch (type)
        {
            case "def": DefaultSpawn(); break;
            case "zerg": ZergSpawn(); break;
            case "shooter": ShooterSpawn(); break;
            case "turtle": TurtleSpawn(); break;
            case "shooter-turtle": ShooterTurtleSpawn(); break;
            case "boss": BossSpawn(); break;
            case "only-boss": OnlyBossSpawn(); break;
        }
        
        double nextWaveTime = DefaultWaveTime + (WaveNumber-1)*2 ; //40,42,44,46,48,50,52,54...
        NextWaveCooldown = new(nextWaveTime, false, true, NextWaveSpawn);
        
        Network.SendToAll(new ClientBattleWorld.ClientBattleWorld.SC_WaveStartedPacket(WaveNumber));
    }

    private void SpawnEnemies(EnemyInfoStorage.EnemyType enemyType, int count)
    {
        _enemySpawner.AddSpawnTask(new EnemySpawnRingTask(enemyType, count, 0, Mathf.DegToRad(360), Vec(0, 0), 2500, 4200));
    }

    private void DefaultSpawn()
    {
        SpawnEnemies(Zerg, WaveNumber*2 + 18); //20,22,24,26,28,30,32,34...
        SpawnEnemies(Shooter, (int) Math.Round(WaveNumber*1.5) + 3); //5,6,7,9,11,12,13,15...
        SpawnEnemies(Turtle, (WaveNumber-1)/2 + 1); //1,1,2,2,3,3,4,4...
        SpawnEnemies(Boss, (WaveNumber-1)/10);
    }
    
    private void ZergSpawn()
    {
        SpawnEnemies(Zerg, WaveNumber*8 + 36); //44,52,60,68
        SpawnEnemies(Shooter, WaveNumber + 3); //4,5,6,7
        SpawnEnemies(Turtle, (WaveNumber-1)/4 + 1);//1,1,1,1,2,2,2,2
    }
    
    private void ShooterSpawn()
    {
        SpawnEnemies(Zerg, WaveNumber*2 + 9); //10,12,14,16
        SpawnEnemies(Shooter, WaveNumber*3 + 9); //12,15,18,21
        SpawnEnemies(Turtle, (WaveNumber-1)/4 + 1);//1,1,1,1,2,2,2,2
    }
    
    private void TurtleSpawn()
    {
        SpawnEnemies(Zerg, WaveNumber*2 + 9); //10,12,14,16
        SpawnEnemies(Shooter, WaveNumber + 3); //4,5,6,7
        SpawnEnemies(Turtle, WaveNumber); //1,2,3,4
    }
    
    private void ShooterTurtleSpawn()
    {
        SpawnEnemies(Zerg, WaveNumber*2 + 9); //10,12,14,16
        SpawnEnemies(Shooter, WaveNumber*3 + 9); //12,15,18,21
        SpawnEnemies(Turtle, WaveNumber); //1,2,3,4
    }
    
    private void BossSpawn()
    {
        SpawnEnemies(Zerg, WaveNumber*2 + 9); //10,12,14,16
        SpawnEnemies(Shooter, WaveNumber + 3); //4,5,6,7
        SpawnEnemies(Turtle, (WaveNumber-1)/4 + 1);//1,1,1,1,2,2,2,2
        SpawnEnemies(Boss, (WaveNumber-1)/10);//0,0,0,0,0,0,0,0,0,0,1
    }
    
    private void OnlyBossSpawn()
    {
        SpawnEnemies(Boss, (WaveNumber-1)/10 + 1);//1,1,1,1,1,1,1,1,1,1,2
    }
 }