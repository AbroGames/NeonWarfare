public readonly struct EnemyProcessEvent(Enemy enemy, double delta)
{
    public Enemy Enemy { get; } = enemy;
    public double Delta { get; } = delta;
}