using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.MapGenerator;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;

public partial class ServerBattleWorld : ServerWorld
{

    public EnemyWave EnemyWave { get; private set; }
    private EnemySpawner _enemySpawner;

    public override void _Ready()
    {
        base._Ready();

        _enemySpawner = new(this);
        EnemyWave = new(_enemySpawner);
    }
    
    protected override void InitMap()
    {
        MapGenerator mapGenerator = new MapGenerator();
        foreach (var location in mapGenerator.Generate())
        {
            foreach (var entity in location.Entities)
            {
                InitEntity(entity);
                Network.SendToAll(entity);
            }
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        EnemyWave.Update(delta);
        _enemySpawner.Update(delta);
    }
}
