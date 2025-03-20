using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

using static NeonWarfare.Scenes.World.ClientWorld.SC_EnemySpawnPacket;

namespace NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld.EnemySpawn;

public record EnemySpawnRectTask : EnemySpawnTask
{
    public Vector2 CenterPosition { get; init; }
    public Vector2 Size { get; init; }

    public EnemySpawnRectTask(EnemyType enemyType, int count, float rotation, float rotationScatter, Vector2 centerPosition, Vector2 size) : base(enemyType, count, rotation, rotationScatter)
    {
        CenterPosition = centerPosition;
        Size = size;
    }

    public override Vector2 GenSpawnPoint()
    {
        return CenterPosition + Vec(Rand.Range(-Size.X/2, Size.X/2), Rand.Range(-Size.Y/2, Size.Y/2));
    }
}