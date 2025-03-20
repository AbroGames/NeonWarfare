using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

using static NeonWarfare.Scenes.World.ClientWorld.SC_EnemySpawnPacket;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

public abstract record EnemySpawnTask
{
    public EnemyType EnemyType { get; init; }
    public int Count { get; init; }
    public float Rotation { get; init; }
    public float RotationScatter { get; init; }

    protected EnemySpawnTask(EnemyType enemyType, int count, float rotation, float rotationScatter)
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