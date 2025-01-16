using Godot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;

public partial class ServerBattleWorld : ServerWorld
{

    private EnemySpawner _enemySpawner;

    public override void _Ready()
    {
        base._Ready();

        _enemySpawner = new(this);
        _enemySpawner.AddSpawnTask(new EnemySpawnRectTask(10, 0, Mathf.DegToRad(360), Vec(0, 0), Vec(200, 200)));
        _enemySpawner.AddSpawnTask(new EnemySpawnRectTask(100, 0, Mathf.DegToRad(360), Vec(-500, 0), Vec(0, 0)));
        _enemySpawner.AddSpawnTask(new EnemySpawnRectTask(10, 0, Mathf.DegToRad(0), Vec(500, 0), Vec(200, 200)));
        _enemySpawner.AddSpawnTask(new EnemySpawnRingTask(100, 0, Mathf.DegToRad(360), Vec(0, 1000), 400, 400));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        _enemySpawner.Update(delta);
    }
}
