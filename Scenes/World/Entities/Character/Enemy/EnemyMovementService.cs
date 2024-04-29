using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class EnemyMovementService
{

    [EventListener(ListenerSide.Server)]
    public void OnEnemyStartAttractionEvent(EnemyStartAttractionEvent attractionEvent)
    {
        attractionEvent.BattleWorld.EnemyAttractors.Add(attractionEvent.Enemy);
    }

    [EventListener(ListenerSide.Server)]
    public void OnEnemyStopAttractionEvent(EnemyStopAttractionEvent attractionEvent)
    {
        attractionEvent.BattleWorld.EnemyAttractors.Remove(attractionEvent.Enemy);
    }

    [EventListener(ListenerSide.Server)]
    public void OnEnemyAboutToTeleport(EnemyAboutToTeleportEvent e)
    {
        var enemy = e.Enemy;
        var target = enemy.Target;

        if (enemy.DistanceTo(target) > 2500)
        {
            enemy.Modulate = enemy.Modulate with { A = 0 };
            var direction = enemy.DirectionTo(target);
            enemy.Position = target.Position + direction * 1500;
            var tween = enemy.CreateTween();
            tween.TweenProperty(enemy, "modulate:a", 1, 1);
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnEnemyPhysicsProcessEvent(EnemyPhysicsProcessEvent enemyPhysicsProcessEvent)
    {
        var (enemy, delta) = enemyPhysicsProcessEvent;
        
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

        long nid = Root.Instance.NetworkEntityManager.GetNid(enemy);
        Network.SendPacketToClients(new ServerPositionEntityPacket(nid, enemy.Position.X, enemy.Position.Y, enemy.Rotation));
    }
    
    private Vector2 GetForwardDirection(Enemy enemy)
    {
        return Vector2.FromAngle(enemy.Rotation - Mathf.Pi / 2);
    }

    private Vector2 GetAttractionDirection(Enemy enemy)
    {
        if ((enemy.GetParent() as BattleWorld).EnemyAttractors.Count == 0) return Vec();
            
        Enemy closestAttractor = (enemy.GetParent() as BattleWorld).EnemyAttractors.FirstOrDefault();
        double dist = closestAttractor.Position.DistanceSquaredTo(enemy.Position);
        foreach (var attractor in (enemy.GetParent() as BattleWorld).EnemyAttractors)
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