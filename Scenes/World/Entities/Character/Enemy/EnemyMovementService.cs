using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Godot;

[GameService]
public class EnemyMovementService
{
    private HashSet<Enemy> _attractors = new();
    
    public EnemyMovementService()
    {
        EventBus.Subscribe<EnemyPhysicsProcessEvent>(OnEnemyPhysicsProcessEvent);
        //EventBus.Subscribe<EnemyStartAttractionEvent>(OnEnemyStartAttractionEvent);
        //EventBus.Subscribe<EnemyStopAttractionEvent>(OnEnemyStopAttractionEvent);
    }

    [GameEventListener]
    public void OnEnemyStartAttractionEvent(EnemyStartAttractionEvent attractionEvent)
    {
        _attractors.Add(attractionEvent.Enemy);
    }

    [GameEventListener]
    public void OnEnemyStopAttractionEvent(EnemyStopAttractionEvent attractionEvent)
    {
        _attractors.Remove(attractionEvent.Enemy);
    }

    [GameEventListener]
    public void OnReset(GameResetEvent reset)
    {
        _attractors = new();
    }
    
    public void OnEnemyPhysicsProcessEvent(EnemyPhysicsProcessEvent enemyPhysicsProcessEvent) {
        MoveForward(enemyPhysicsProcessEvent.Enemy, enemyPhysicsProcessEvent.Delta);
    }
    
    public void MoveForward(Enemy enemy, double delta)
    {
        Vector2 directionToMove = GetForwardDirection(enemy);
        Vector2 attractionDirection = Vec();
        
        var velDir = directionToMove;
        if (!enemy.IsBoss)
        {
            var minSpeedFactor = 0.9;
            attractionDirection = GetAttractionDirection(enemy) * 0.35;
            velDir += attractionDirection;
            if (velDir.LengthSquared() < 1)
            {
                velDir = velDir.Normalized() * minSpeedFactor;
            }
        }
        // Переместить и првоерить физику
        enemy.Velocity = velDir * enemy.MovementSpeed;
        enemy.MoveAndSlide();
    }
    
    public Vector2 GetForwardDirection(Enemy enemy)
    {
        return Vector2.FromAngle(enemy.Rotation - Mathf.Pi / 2);
    }

    public Vector2 GetAttractionDirection(Enemy enemy)
    {
        if (_attractors.Count == 0) return Vec();
            
        Enemy closestAttractor = _attractors.FirstOrDefault();
        double dist = closestAttractor.Position.DistanceSquaredTo(enemy.Position);
        foreach (var attractor in _attractors)
        {
            if (attractor == enemy) continue;
            var newDist = enemy.Position.DistanceSquaredTo(attractor.Position);
            if (newDist < dist)
            {
                closestAttractor = attractor;
                dist = newDist;
            }
        }

        return enemy.DirectionTo(closestAttractor);
    }
}