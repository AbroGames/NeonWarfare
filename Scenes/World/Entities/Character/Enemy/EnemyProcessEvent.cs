public class EnemyProcessEvent(Enemy enemy, double delta)
{
    public Enemy Enemy { get; private set; } = enemy;
    public double Delta { get; private set; } = delta;
}