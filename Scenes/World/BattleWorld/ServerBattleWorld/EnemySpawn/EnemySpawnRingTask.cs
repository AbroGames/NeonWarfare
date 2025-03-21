using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

public record EnemySpawnRingTask : EnemySpawnTask
{
    public Vector2 CenterPosition { get; init; }
    public int MinDist { get; init; }
    public int MaxDist { get; init; }

    public EnemySpawnRingTask(EnemyInfoStorage.EnemyType enemyType, int count, float rotation, float rotationScatter, Vector2 centerPosition, int minDist, int maxDist) : base(enemyType, count, rotation, rotationScatter)
    {
        CenterPosition = centerPosition;
        MinDist = minDist;
        MaxDist = maxDist;
    }

    public override Vector2 GenSpawnPoint()
    {
        return CenterPosition + Rand.InsideAnnulus(MinDist, MaxDist);
    }
}