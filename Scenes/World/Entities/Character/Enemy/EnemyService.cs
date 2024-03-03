using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class EnemyService
{
    [EventListener]
    public void OnEnemyReady(EnemyReadyEvent e)
    {
        var enemy = e.Enemy;

        enemy.PrimaryCd.Duration = 1;
        enemy.PrimaryCd.Ready += () =>
        {
            if (CanSeePlayer(enemy))
            {
                EventBus.Publish(new EnemyAttackEvent(enemy));
            }
        };
        
        enemy.TeleportCd.Ready += () =>
        {
            EventBus.Publish(new EnemyAboutToTeleportEvent(enemy));
        };
    }
    
    private bool CanSeePlayer(Enemy enemy)
    {
        var collider = enemy.RayCast.GetCollider();
        return collider is Player;
    }

    [EventListener]
    public void OnEnemyProcessEvent(EnemyProcessEvent e)
    {
        var (enemy, delta) = e;
        enemy.TeleportCd.Update(delta);
    }
}