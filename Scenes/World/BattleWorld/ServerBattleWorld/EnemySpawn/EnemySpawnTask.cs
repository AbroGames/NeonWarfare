using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

public abstract record EnemySpawnTask
{
    public EnemyInfoStorage.EnemyType EnemyType { get; init; }
    public int Count { get; init; }
    public float Rotation { get; init; }
    public float RotationScatter { get; init; }

    protected EnemySpawnTask(EnemyInfoStorage.EnemyType enemyType, int count, float rotation, float rotationScatter)
    {
        EnemyType = enemyType;
        Count = count;
        Rotation = rotation;
        RotationScatter = rotationScatter;
    }

    public float GenRotation()
    {
        return Rotation + Rand.Range(-RotationScatter/2, RotationScatter/2);
    }
    
    public abstract Vector2 GenSpawnPoint();
}