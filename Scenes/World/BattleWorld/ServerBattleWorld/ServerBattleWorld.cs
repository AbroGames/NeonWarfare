using Godot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

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

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        EnemyWave.Update(delta);
        _enemySpawner.Update(delta);
    }
}
