using Godot;

public class EnemyMovementService
{
    public EnemyMovementService()
    {
        Root.Instance.EventBus.Subscribe<EnemyPhysicsProcessEvent>(OnEnemyPhysicsProcessEvent);
    }
    
    public void OnEnemyPhysicsProcessEvent(EnemyPhysicsProcessEvent enemyPhysicsProcessEvent) {
        MoveForward(enemyPhysicsProcessEvent.Enemy, enemyPhysicsProcessEvent.Delta);
    }
    
    private void MoveForward(Enemy enemy, double delta)
    {
        Vector2 directionToMove = GetForwardDirection(enemy);
        // Переместить и првоерить физику
        enemy.MoveAndCollide(directionToMove * enemy.MovementSpeed * delta);
    }
    
    private Vector2 GetForwardDirection(Enemy enemy)
    {
        return Vector2.FromAngle(enemy.Rotation - Mathf.Pi / 2);
    }
    
}