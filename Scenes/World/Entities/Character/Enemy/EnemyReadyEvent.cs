public readonly struct EnemyReadyEvent(Enemy enemy)
{
    public Enemy Enemy { get; } = enemy;
}