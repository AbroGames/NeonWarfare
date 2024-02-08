public readonly struct EnemyPhysicsProcessEvent(Enemy enemy, double delta)
{
    public Enemy Enemy { get; } = enemy;
    public double Delta { get; } = delta;
}